#!/usr/bin/env node

import fs from "node:fs";
import process from "node:process";
import Ajv2020 from "ajv/dist/2020.js";
import addFormats from "ajv-formats";

const [schemaPath, ...documentPaths] = process.argv.slice(2);
if (!schemaPath || documentPaths.length === 0) {
  console.error(
    "usage: validate_evidence_schema.mjs <schema.json> <document.json> [...]",
  );
  process.exit(2);
}

let schema;
try {
  schema = JSON.parse(fs.readFileSync(schemaPath, "utf8"));
} catch (error) {
  console.error(`schema:${schemaPath}: ${error.message}`);
  process.exit(2);
}

const ajv = new Ajv2020({
  allErrors: true,
  strict: true,
  validateFormats: true,
});
addFormats(ajv);

let validate;
try {
  validate = ajv.compile(schema);
} catch (error) {
  console.error(`schema:${schemaPath}: ${error.message}`);
  process.exit(2);
}

let failed = false;
for (const documentPath of documentPaths) {
  let document;
  try {
    document = JSON.parse(fs.readFileSync(documentPath, "utf8"));
  } catch (error) {
    console.error(`${documentPath}: ${error.message}`);
    failed = true;
    continue;
  }
  if (!validate(document)) {
    failed = true;
    for (const error of validate.errors ?? []) {
      console.error(
        `${documentPath}:${error.instancePath || "/"} ${error.message}`,
      );
    }
  }
}

process.exit(failed ? 1 : 0);
