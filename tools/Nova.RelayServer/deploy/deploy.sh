#!/usr/bin/env bash
set -Eeuo pipefail
IFS=$'\n\t'
PATH=/usr/sbin:/usr/bin:/sbin:/bin
export PATH
umask 077

readonly SERVICE_NAME="hashkrieg-relay.service"
readonly SERVICE_USER="novarelay"
readonly SERVICE_GROUP="novarelay"
readonly BASE_DIR="/opt/hashkrieg-relay"
readonly RELEASES_DIR="${BASE_DIR}/releases"
readonly CURRENT_LINK="${BASE_DIR}/current"
readonly PREVIOUS_LINK="${BASE_DIR}/previous"
readonly UNIT_PATH="/etc/systemd/system/${SERVICE_NAME}"
readonly LIVE_ENV_PATH="/etc/hashkrieg-relay.env"
readonly ENV_EXAMPLE_PATH="/etc/hashkrieg-relay.env.example"
readonly LOCK_PATH="/run/hashkrieg-relay-deploy.lock"
readonly READY_TIMEOUT_SECONDS=30
readonly READY_STABLE_CHECKS=5
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
readonly SCRIPT_DIR

staging_dir=""
unit_backup=""
unit_prior_state=""

usage() {
    printf '%s\n' \
        "usage: deploy.sh bootstrap" \
        "       deploy.sh deploy <archive.tar.gz> <archive.sha256>" \
        "       deploy.sh rollback" >&2
}

die() {
    printf 'hashkrieg-relay deploy: %s\n' "$1" >&2
    exit 1
}

note() {
    printf 'hashkrieg-relay deploy: %s\n' "$1"
}

cleanup() {
    if [[ -n "${staging_dir}" && "${staging_dir}" == "${BASE_DIR}/.staging."* \
        && -d "${staging_dir}" ]]; then
        rm -rf -- "${staging_dir}"
    fi
    if [[ -n "${unit_backup}" && "${unit_backup}" == /run/hashkrieg-relay-unit.* \
        && -f "${unit_backup}" ]]; then
        rm -f -- "${unit_backup}"
    fi
}
trap cleanup EXIT

require_command() {
    command -v -- "$1" >/dev/null 2>&1 || die "required command is unavailable: $1"
}

preflight_platform() {
    [[ "${EUID}" -eq 0 ]] || die "must run as root"
    [[ "$(uname -s)" == "Linux" ]] || die "requires Linux"
    [[ "$(uname -m)" == "x86_64" ]] || die "requires Linux x86_64"

    local command_name
    for command_name in \
        awk basename chmod chown cp dirname find flock getent grep groupadd id \
        install journalctl ln mkdir mktemp mv python3 readlink rm sha256sum sleep \
        stat systemctl uname unlink useradd; do
        require_command "${command_name}"
    done

    [[ -d /run && ! -L /run && "$(stat -c '%u' -- /run)" == "0" ]] \
        || die "/run must be a root-owned regular directory"
    local run_mode
    run_mode="$(stat -c '%a' -- /run)"
    [[ "${run_mode}" =~ ^[0-7]{3,4}$ ]] \
        || die "/run mode is invalid"
    (( (8#${run_mode} & 022) == 0 )) \
        || die "/run must not be group- or world-writable"
    if [[ -e "${LOCK_PATH}" || -L "${LOCK_PATH}" ]]; then
        [[ -f "${LOCK_PATH}" && ! -L "${LOCK_PATH}" \
            && "$(stat -c '%u' -- "${LOCK_PATH}")" == "0" \
            && "$(stat -c '%h' -- "${LOCK_PATH}")" == "1" ]] \
            || die "deployment lock must be a root-owned, unlinked regular file"
    fi
    exec 9>"${LOCK_PATH}"
    chmod 0600 -- "${LOCK_PATH}"
    flock -n 9 || die "another relay deployment holds the lock"
}

validate_live_environment() {
    [[ -f "${LIVE_ENV_PATH}" && ! -L "${LIVE_ENV_PATH}" ]] \
        || die "the live environment file must be provisioned manually before activation"
    [[ "$(stat -c '%u' -- "${LIVE_ENV_PATH}")" == "0" ]] \
        || die "the live environment file must be owned by root"
    local mode
    mode="$(stat -c '%a' -- "${LIVE_ENV_PATH}")"
    [[ "${mode}" =~ ^[0-7]{3,4}$ ]] \
        || die "the live environment file mode is invalid"
    (( (8#${mode} & 077) == 0 )) \
        || die "the live environment file must not grant group or world permissions"
}

validate_release_target() {
    local target="$1"
    [[ "${target}" =~ ^${RELEASES_DIR}/[0-9a-f]{40}$ && -d "${target}" ]] \
        || die "release symlink target is outside the immutable release store"
}

capture_link() {
    local link_path="$1"
    if [[ -L "${link_path}" ]]; then
        local target
        target="$(readlink -- "${link_path}")"
        validate_release_target "${target}"
        printf '%s' "${target}"
        return
    fi
    [[ ! -e "${link_path}" ]] || die "managed path is not a symbolic link"
}

atomic_set_link() {
    local link_path="$1"
    local target="$2"
    validate_release_target "${target}"
    local temporary_link="${link_path}.new.$$"
    [[ ! -e "${temporary_link}" && ! -L "${temporary_link}" ]] \
        || return 1
    ln -s -- "${target}" "${temporary_link}" || return 1
    mv -Tf -- "${temporary_link}" "${link_path}" || return 1
}

restore_link() {
    local link_path="$1"
    local target="$2"
    if [[ -n "${target}" ]]; then
        atomic_set_link "${link_path}" "${target}"
    elif [[ -L "${link_path}" ]]; then
        unlink -- "${link_path}"
    else
        [[ ! -e "${link_path}" ]]
    fi
}

install_unit() {
    local source_unit="$1"
    [[ -f "${source_unit}" && ! -L "${source_unit}" ]] || return 1
    local temporary_unit="${UNIT_PATH}.new.$$"
    install -o root -g root -m 0644 -- "${source_unit}" "${temporary_unit}" \
        || return 1
    mv -Tf -- "${temporary_unit}" "${UNIT_PATH}" || return 1
}

capture_unit() {
    if [[ -e "${UNIT_PATH}" ]]; then
        [[ -f "${UNIT_PATH}" && ! -L "${UNIT_PATH}" ]] \
            || die "managed systemd unit is not a regular file"
        unit_backup="$(mktemp /run/hashkrieg-relay-unit.XXXXXX)"
        cp --preserve=mode,ownership,timestamps -- "${UNIT_PATH}" "${unit_backup}"
        unit_prior_state="present"
    else
        unit_prior_state="absent"
    fi
}

restore_unit() {
    local prior_state="$1"
    if [[ "${prior_state}" == "present" ]]; then
        install_unit "${unit_backup}"
    elif [[ "${prior_state}" == "absent" ]]; then
        rm -f -- "${UNIT_PATH}"
    else
        return 1
    fi
}

wait_until_ready() {
    local deadline=$((SECONDS + READY_TIMEOUT_SECONDS))
    local stable_checks=0
    local stable_invocation=""
    while (( SECONDS < deadline )); do
        systemctl is-failed --quiet "${SERVICE_NAME}" && return 1

        if ! systemctl is-active --quiet "${SERVICE_NAME}"; then
            stable_checks=0
            stable_invocation=""
            systemctl is-failed --quiet "${SERVICE_NAME}" && return 1
            sleep 1
            continue
        fi

        local invocation_id
        if ! invocation_id="$(
                systemctl show --property InvocationID --value "${SERVICE_NAME}"
            )"; then
            stable_checks=0
            stable_invocation=""
            systemctl is-failed --quiet "${SERVICE_NAME}" && return 1
            sleep 1
            continue
        fi
        if [[ ! "${invocation_id}" =~ ^[0-9a-f]{32}$ ]]; then
            stable_checks=0
            stable_invocation=""
            systemctl is-failed --quiet "${SERVICE_NAME}" && return 1
            sleep 1
            continue
        fi
        if [[ "${invocation_id}" != "${stable_invocation}" ]]; then
            stable_checks=0
            stable_invocation="${invocation_id}"
        fi

        if ! journalctl --quiet --no-pager -u "${SERVICE_NAME}" \
                "_SYSTEMD_INVOCATION_ID=${invocation_id}" -o cat 2>/dev/null \
                | grep -F '[Relay] ready on ' >/dev/null; then
            stable_checks=0
            systemctl is-failed --quiet "${SERVICE_NAME}" && return 1
            sleep 1
            continue
        fi

        # Re-read service state and invocation after the journal query so a
        # restart between observation and counting cannot inherit readiness.
        systemctl is-failed --quiet "${SERVICE_NAME}" && return 1
        if ! systemctl is-active --quiet "${SERVICE_NAME}"; then
            stable_checks=0
            stable_invocation=""
            systemctl is-failed --quiet "${SERVICE_NAME}" && return 1
            sleep 1
            continue
        fi
        local confirmed_invocation
        if ! confirmed_invocation="$(
                systemctl show --property InvocationID --value "${SERVICE_NAME}"
            )"; then
            stable_checks=0
            stable_invocation=""
            systemctl is-failed --quiet "${SERVICE_NAME}" && return 1
            sleep 1
            continue
        fi
        if [[ "${confirmed_invocation}" != "${invocation_id}" ]]; then
            stable_checks=0
            if [[ "${confirmed_invocation}" =~ ^[0-9a-f]{32}$ ]]; then
                stable_invocation="${confirmed_invocation}"
            else
                stable_invocation=""
            fi
            systemctl is-failed --quiet "${SERVICE_NAME}" && return 1
            sleep 1
            continue
        fi

        stable_checks=$((stable_checks + 1))
        if (( stable_checks >= READY_STABLE_CHECKS )); then
            return 0
        fi
        systemctl is-failed --quiet "${SERVICE_NAME}" && return 1
        sleep 1
    done
    return 1
}

restore_transaction() {
    local prior_unit_state="$1"
    local prior_current="$2"
    local prior_previous="$3"
    local failed=0

    restore_unit "${prior_unit_state}" || failed=1
    restore_link "${CURRENT_LINK}" "${prior_current}" || failed=1
    restore_link "${PREVIOUS_LINK}" "${prior_previous}" || failed=1
    systemctl daemon-reload || failed=1
    if [[ -n "${prior_current}" ]]; then
        systemctl restart "${SERVICE_NAME}" || failed=1
        wait_until_ready || failed=1
    else
        systemctl stop "${SERVICE_NAME}" >/dev/null 2>&1 || true
    fi
    return "${failed}"
}

activate_transaction() {
    local target_release="$1"
    local next_previous="$2"
    install_unit "${target_release}/deploy/hashkrieg-relay.service" || return 1
    if [[ -n "${next_previous}" ]]; then
        atomic_set_link "${PREVIOUS_LINK}" "${next_previous}" || return 1
    else
        restore_link "${PREVIOUS_LINK}" "" || return 1
    fi
    atomic_set_link "${CURRENT_LINK}" "${target_release}" || return 1
    systemctl daemon-reload || return 1
    systemctl restart "${SERVICE_NAME}" || return 1
    wait_until_ready || return 1
}

copy_deploy_inputs() {
    local archive_source="$1"
    local checksum_source="$2"
    local archive_destination="$3"
    local checksum_destination="$4"
    python3 - \
        "${archive_source}" "${checksum_source}" \
        "${archive_destination}" "${checksum_destination}" <<'PY'
import os
import stat
import sys

inputs = (
    (sys.argv[1], sys.argv[3], 536_870_912),
    (sys.argv[2], sys.argv[4], 4_096),
)

def copy_stable_regular(source_path, destination_path, maximum_size):
    source_fd = None
    destination_fd = None
    try:
        source_fd = os.open(source_path, os.O_RDONLY | os.O_NOFOLLOW)
        before = os.fstat(source_fd)
        if (not stat.S_ISREG(before.st_mode)
                or before.st_size <= 0 or before.st_size > maximum_size):
            raise ValueError
        destination_fd = os.open(
            destination_path,
            os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW,
            0o600,
        )
        copied = 0
        while True:
            chunk = os.read(source_fd, min(1024 * 1024, maximum_size + 1 - copied))
            if not chunk:
                break
            copied += len(chunk)
            if copied > maximum_size:
                raise ValueError
            view = memoryview(chunk)
            while view:
                written = os.write(destination_fd, view)
                if written <= 0:
                    raise OSError
                view = view[written:]
        os.fsync(destination_fd)
        after = os.fstat(source_fd)
        stable_fields = (
            "st_dev", "st_ino", "st_mode", "st_uid", "st_gid", "st_size",
            "st_mtime_ns", "st_ctime_ns",
        )
        if (copied != before.st_size
                or any(getattr(before, field) != getattr(after, field)
                       for field in stable_fields)):
            raise ValueError
    except (OSError, ValueError):
        raise SystemExit("deployment input could not be copied safely")
    finally:
        if destination_fd is not None:
            os.close(destination_fd)
        if source_fd is not None:
            os.close(source_fd)

for source, destination, size_limit in inputs:
    copy_stable_regular(source, destination, size_limit)
PY
}

verify_outer_checksum() {
    local archive="$1"
    local checksum_file="$2"
    local expected_archive_name="$3"
    [[ -f "${archive}" && ! -L "${archive}" ]] \
        || die "archive must be a regular non-symbolic file"
    [[ -f "${checksum_file}" && ! -L "${checksum_file}" ]] \
        || die "checksum must be a regular non-symbolic file"
    local archive_size
    archive_size="$(stat -c '%s' -- "${archive}")"
    [[ "${archive_size}" =~ ^[0-9]+$ && "${archive_size}" -gt 0 \
        && "${archive_size}" -le 536870912 ]] \
        || die "archive size is outside the accepted range"

    local -a checksum_lines=()
    mapfile -t checksum_lines < "${checksum_file}"
    [[ "${#checksum_lines[@]}" -eq 1 ]] \
        || die "checksum file must contain exactly one canonical line"
    local line="${checksum_lines[0]}"
    [[ "${line}" =~ ^([0-9a-f]{64})[[:space:]][[:space:]]([^/[:space:]]+)$ ]] \
        || die "checksum file is not canonical"
    [[ "${BASH_REMATCH[2]}" == "${expected_archive_name}" ]] \
        || die "checksum filename does not match the archive"
    local actual
    actual="$(sha256sum -- "${archive}")"
    actual="${actual%% *}"
    [[ "${actual}" == "${BASH_REMATCH[1]}" ]] || die "archive checksum mismatch"
}

extract_safe_bundle() {
    local archive="$1"
    local destination="$2"
    python3 - "${archive}" "${destination}" <<'PY'
import pathlib
import re
import shutil
import sys
import tarfile

archive = pathlib.Path(sys.argv[1])
destination = pathlib.Path(sys.argv[2])
allowed_deploy = {
    "deploy/deploy.sh",
    "deploy/hashkrieg-relay.env.example",
    "deploy/hashkrieg-relay.service",
}
required_files = allowed_deploy | {
    "app/nova-relay",
    "BUILD_INFO",
    "SHA256SUMS",
}
safe_name = re.compile(r"^[A-Za-z0-9._+/-]+$")
seen = set()
total_size = 0

with tarfile.open(archive, mode="r:gz") as bundle:
    members = bundle.getmembers()
    if not members or len(members) > 4096:
        raise SystemExit("bundle entry count is outside the accepted range")
    for member in members:
        name = member.name
        parts = pathlib.PurePosixPath(name).parts
        normalized = "/".join(parts)
        if (not safe_name.fullmatch(name) or name.startswith("/") or "\\" in name
                or not parts or any(part in ("", ".", "..") for part in parts)
                or name.rstrip("/") != normalized or normalized in seen):
            raise SystemExit("bundle contains an unsafe or duplicate path")
        seen.add(normalized)
        name = normalized
        is_regular = member.type in (tarfile.REGTYPE, tarfile.AREGTYPE)
        is_directory = member.type == tarfile.DIRTYPE
        if not (is_regular or is_directory):
            raise SystemExit("bundle contains a link or special file")
        if parts[0] == "app":
            if len(parts) == 1 and not is_directory:
                raise SystemExit("bundle app root is not a directory")
        elif parts[0] == "deploy":
            if len(parts) == 1:
                if not is_directory:
                    raise SystemExit("bundle deploy root is not a directory")
            elif name not in allowed_deploy or not is_regular:
                raise SystemExit("bundle contains an unexpected deploy artifact")
        elif name not in ("BUILD_INFO", "SHA256SUMS") or not is_regular:
            raise SystemExit("bundle contains an unexpected top-level artifact")
        if is_regular:
            if member.size < 0 or member.size > 268435456:
                raise SystemExit("bundle file size is outside the accepted range")
            if ((name == "BUILD_INFO" and member.size > 256)
                    or (name == "SHA256SUMS" and member.size > 1048576)
                    or (name.startswith("deploy/") and member.size > 1048576)):
                raise SystemExit("bundle metadata size exceeds the accepted range")
            total_size += member.size
            if total_size > 536870912:
                raise SystemExit("bundle expanded size exceeds the accepted range")

    if not required_files.issubset(seen):
        raise SystemExit("bundle is missing a required artifact")

    for member in sorted(members, key=lambda item: (len(pathlib.PurePosixPath(item.name).parts), item.name)):
        target = destination.joinpath(*pathlib.PurePosixPath(member.name).parts)
        if member.isdir():
            target.mkdir(mode=0o700, parents=True, exist_ok=True)
            continue
        target.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
        source = bundle.extractfile(member)
        if source is None:
            raise SystemExit("bundle file could not be read")
        with source, target.open("xb") as output:
            shutil.copyfileobj(source, output, length=1024 * 1024)
        if target.stat().st_size != member.size:
            raise SystemExit("bundle file length changed during extraction")
PY
}

verify_inner_bundle() {
    local destination="$1"
    python3 - "${destination}" <<'PY'
import hashlib
import os
import pathlib
import re
import sys

root = pathlib.Path(sys.argv[1])
manifest_path = root / "SHA256SUMS"
line_pattern = re.compile(r"^([0-9a-f]{64})  ([A-Za-z0-9._+/-]+)$")
expected = {}
try:
    lines = manifest_path.read_text(encoding="utf-8").splitlines()
except (OSError, UnicodeError):
    raise SystemExit("inner checksum manifest is unreadable")
if not lines:
    raise SystemExit("inner checksum manifest is empty")
for line in lines:
    match = line_pattern.fullmatch(line)
    if not match:
        raise SystemExit("inner checksum manifest is not canonical")
    relative = match.group(2)
    parts = pathlib.PurePosixPath(relative).parts
    if (relative == "SHA256SUMS" or relative.startswith("/") or not parts
            or any(part in ("", ".", "..") for part in parts)
            or relative in expected):
        raise SystemExit("inner checksum manifest contains an unsafe or duplicate path")
    expected[relative] = match.group(1)

actual = set()
for directory, directories, files in os.walk(root, followlinks=False):
    for name in directories:
        if (pathlib.Path(directory) / name).is_symlink():
            raise SystemExit("extracted bundle contains a symbolic link")
    for name in files:
        path = pathlib.Path(directory) / name
        if path.is_symlink():
            raise SystemExit("extracted bundle contains a symbolic link")
        relative = path.relative_to(root).as_posix()
        if relative != "SHA256SUMS":
            actual.add(relative)
if actual != set(expected):
    raise SystemExit("inner checksum manifest does not cover the exact file set")
for relative, digest in expected.items():
    calculated_hash = hashlib.sha256()
    with (root / relative).open("rb") as source:
        while chunk := source.read(1024 * 1024):
            calculated_hash.update(chunk)
    calculated = calculated_hash.hexdigest()
    if calculated != digest:
        raise SystemExit("inner bundle checksum mismatch")

try:
    build_lines = (root / "BUILD_INFO").read_text(encoding="ascii").splitlines()
except (OSError, UnicodeError):
    raise SystemExit("BUILD_INFO is unreadable")
if (len(build_lines) != 3
        or not re.fullmatch(r"commit_sha=[0-9a-f]{40}", build_lines[0])
        or build_lines[1] != "sdk_version=8.0.318"
        or build_lines[2] != "runtime=linux-x64"):
    raise SystemExit("BUILD_INFO is not canonical")
print(build_lines[0].split("=", 1)[1])
PY
}

normalize_release() {
    local release_dir="$1"
    find "${release_dir}" -type d -exec chmod 0755 -- {} +
    find "${release_dir}" -type f -exec chmod 0644 -- {} +
    chmod 0755 -- "${release_dir}/app/nova-relay" "${release_dir}/deploy/deploy.sh"
    chown -R root:root -- "${release_dir}"
}

bootstrap() {
    [[ -f "${SCRIPT_DIR}/hashkrieg-relay.service" \
        && ! -L "${SCRIPT_DIR}/hashkrieg-relay.service" ]] \
        || die "versioned systemd unit is unavailable"
    [[ -f "${SCRIPT_DIR}/hashkrieg-relay.env.example" \
        && ! -L "${SCRIPT_DIR}/hashkrieg-relay.env.example" ]] \
        || die "versioned environment example is unavailable"

    if ! getent group "${SERVICE_GROUP}" >/dev/null; then
        groupadd --system "${SERVICE_GROUP}"
    fi
    if ! id "${SERVICE_USER}" >/dev/null 2>&1; then
        local nologin_shell
        nologin_shell="$(command -v nologin || true)"
        [[ -n "${nologin_shell}" ]] || die "nologin shell is unavailable"
        useradd --system --gid "${SERVICE_GROUP}" --home-dir /nonexistent \
            --no-create-home --shell "${nologin_shell}" "${SERVICE_USER}"
    fi
    [[ "$(id -u "${SERVICE_USER}")" -ne 0 \
        && "$(id -gn "${SERVICE_USER}")" == "${SERVICE_GROUP}" ]] \
        || die "existing relay identity is not the required unprivileged user/group"

    install -d -o root -g root -m 0755 -- "${BASE_DIR}" "${RELEASES_DIR}"
    install_unit "${SCRIPT_DIR}/hashkrieg-relay.service" \
        || die "systemd unit installation failed"
    install -o root -g root -m 0644 -- \
        "${SCRIPT_DIR}/hashkrieg-relay.env.example" "${ENV_EXAMPLE_PATH}"
    systemctl daemon-reload
    systemctl enable "${SERVICE_NAME}"
    note "bootstrap complete; provision the live environment file manually before deploy"
}

deploy_release() {
    local archive="$1"
    local checksum_file="$2"
    validate_live_environment
    [[ -d "${RELEASES_DIR}" ]] || die "bootstrap must run before deploy"

    staging_dir="$(mktemp -d "${BASE_DIR}/.staging.XXXXXX")"
    chmod 0700 -- "${staging_dir}"
    local incoming_dir="${staging_dir}/incoming"
    local release_stage="${staging_dir}/release"
    mkdir -m 0700 -- "${incoming_dir}" "${release_stage}"
    local staged_archive="${incoming_dir}/archive.tar.gz"
    local staged_checksum="${incoming_dir}/archive.sha256"
    local archive_name
    archive_name="$(basename -- "${archive}")"
    copy_deploy_inputs \
        "${archive}" "${checksum_file}" "${staged_archive}" "${staged_checksum}"
    verify_outer_checksum "${staged_archive}" "${staged_checksum}" "${archive_name}"
    extract_safe_bundle "${staged_archive}" "${release_stage}"
    local commit_sha
    commit_sha="$(verify_inner_bundle "${release_stage}")"
    local release_dir="${RELEASES_DIR}/${commit_sha}"
    [[ ! -e "${release_dir}" ]] \
        || die "immutable release already exists; refusing to overwrite it"
    normalize_release "${release_stage}"
    mv -- "${release_stage}" "${release_dir}"

    local prior_current prior_previous prior_unit_state
    prior_current="$(capture_link "${CURRENT_LINK}")"
    prior_previous="$(capture_link "${PREVIOUS_LINK}")"
    [[ -n "${prior_current}" || -z "${prior_previous}" ]] \
        || die "previous link exists without a current release"
    capture_unit
    prior_unit_state="${unit_prior_state}"

    if activate_transaction "${release_dir}" "${prior_current}"; then
        note "release ${commit_sha} is ready"
        return
    fi

    note "activation failed; restoring unit and release links"
    restore_transaction "${prior_unit_state}" "${prior_current}" "${prior_previous}" \
        || die "activation and rollback both failed"
    die "release failed readiness and was rolled back"
}

rollback_release() {
    validate_live_environment
    local prior_current prior_previous prior_unit_state
    prior_current="$(capture_link "${CURRENT_LINK}")"
    prior_previous="$(capture_link "${PREVIOUS_LINK}")"
    [[ -n "${prior_current}" && -n "${prior_previous}" ]] \
        || die "rollback requires both current and previous releases"
    [[ "${prior_current}" != "${prior_previous}" ]] \
        || die "current and previous releases must be different"
    capture_unit
    prior_unit_state="${unit_prior_state}"

    if activate_transaction "${prior_previous}" "${prior_current}"; then
        note "rollback target is ready"
        return
    fi

    note "rollback target failed readiness; restoring prior state"
    restore_transaction "${prior_unit_state}" "${prior_current}" "${prior_previous}" \
        || die "rollback activation and restoration both failed"
    die "rollback target failed readiness; prior release was restored"
}

main() {
    [[ "$#" -ge 1 ]] || { usage; exit 1; }
    local command_name="$1"
    shift
    case "${command_name}" in
        bootstrap)
            [[ "$#" -eq 0 ]] || { usage; exit 1; }
            ;;
        deploy)
            [[ "$#" -eq 2 ]] || { usage; exit 1; }
            ;;
        rollback)
            [[ "$#" -eq 0 ]] || { usage; exit 1; }
            ;;
        *)
            usage
            exit 1
            ;;
    esac

    preflight_platform
    case "${command_name}" in
        bootstrap) bootstrap ;;
        deploy) deploy_release "$1" "$2" ;;
        rollback) rollback_release ;;
    esac
}

main "$@"
