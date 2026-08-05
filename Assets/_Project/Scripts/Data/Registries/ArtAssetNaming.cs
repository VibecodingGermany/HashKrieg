using System;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;

namespace Nova.Data
{
    /// <summary>
    /// Parser for the art-asset file-name convention of
    /// docs/assets/ArtAssetStandard.md sections 1-2: prefabs are named
    /// <c>PF_UNIT_&lt;Faction&gt;_&lt;Role&gt;.prefab</c> or
    /// <c>PF_BLDG_&lt;Faction&gt;_&lt;Role&gt;.prefab</c>, with the PascalCase
    /// faction tokens <c>Alliance</c>/<c>Legion</c> and the MS-1 role names of
    /// quality/content/mvp-v1.json (which are exactly the <see cref="UnitRole"/>
    /// member names). Pure string logic with no Unity object access, so the
    /// convention is unit-testable without an Editor import.
    /// </summary>
    public static class ArtAssetNaming
    {
        /// <summary>Repository-relative root every art asset lives under (ArtAssetStandard.md section 1).</summary>
        public const string ArtRoot = "Assets/_Project/Art";

        /// <summary>
        /// Parses a prefab file name (with or without extension, without any
        /// directory part) into faction, role and the unit/building class.
        /// Returns false for every name that does not match the convention
        /// exactly — including correctly named meshes, textures and materials,
        /// which are not prefabs and therefore never resolve a view prefab.
        /// </summary>
        public static bool TryParsePrefabName(
            string fileName,
            out FactionId faction,
            out UnitRole role,
            out bool isBuilding)
        {
            faction = default;
            role = default;
            isBuilding = false;

            if (string.IsNullOrEmpty(fileName)) return false;

            string stem = fileName;
            if (stem.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                stem = stem.Substring(0, stem.Length - ".prefab".Length);
            }

            string[] parts = stem.Split('_');
            if (parts.Length != 4) return false;
            if (parts[0] != "PF") return false;

            if (parts[1] == "BLDG")
            {
                isBuilding = true;
            }
            else if (parts[1] != "UNIT")
            {
                return false;
            }

            if (parts[2] == "Alliance")
            {
                faction = FactionId.Alliance;
            }
            else if (parts[2] == "Legion")
            {
                faction = FactionId.Legion;
            }
            else
            {
                return false;
            }

            // TryParse is case-sensitive with ignoreCase: false — the
            // convention fixes exact PascalCase role tokens.
            if (!Enum.TryParse(parts[3], ignoreCase: false, out role)) return false;

            return isBuilding ? IsBuildingRole(role) : IsUnitRole(role);
        }

        /// <summary>
        /// Full prefab-name resolution in one call: parses the convention and
        /// returns the simulation definition id (Alliance = role wire value,
        /// Legion = role + 17; 0 is never returned on success). This is the
        /// entry point for tooling assemblies that must not reference
        /// Nova.Simulation directly (e.g. Nova.Editor, whose reference list is
        /// pinned by the architecture check).
        /// </summary>
        public static bool TryParsePrefabDefinitionId(string fileName, out int definitionId, out bool isBuilding)
        {
            definitionId = 0;
            if (!TryParsePrefabName(fileName, out FactionId faction, out UnitRole role, out isBuilding))
            {
                return false;
            }

            definitionId = SimDefinitions.ToDefinitionId(faction, role);
            return definitionId != 0;
        }

        /// <summary>The nine MS-1 building roles (HQ..DefensePlatform, wire values 3-11).</summary>
        public static bool IsBuildingRole(UnitRole role)
        {
            return role >= UnitRole.HQ && role <= UnitRole.DefensePlatform;
        }

        /// <summary>The eight MS-1 unit roles: Builder/Harvester (1-2) and BasicInfantry..Artillery (12-17). Unit (0) is the generic construction-site role and belongs to neither set.</summary>
        public static bool IsUnitRole(UnitRole role)
        {
            return role == UnitRole.Builder || role == UnitRole.Harvester
                || (role >= UnitRole.BasicInfantry && role <= UnitRole.Artillery);
        }
    }
}
