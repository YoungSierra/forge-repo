/** Smoke: Unity CLI JSON envelope parsing (no live `unity` binary required). */
import {
  isUnityCliIntegrationEnabled,
  parseCliJsonEnvelope,
  preferUnityTestViaCli,
  unwrapCliData,
} from "../src/agent/unityCli.js";

const errors = [];
function ok(cond, msg) {
  if (cond) console.log("OK ", msg);
  else {
    console.error("FAIL", msg);
    errors.push(msg);
  }
}

const envelope = parseCliJsonEnvelope(
  JSON.stringify({ success: true, data: { version: "6000.0.47f1" }, errors: [], warnings: [] }),
);
ok(envelope?.success === true, "parse plain JSON envelope");
ok(unwrapCliData(envelope)?.version === "6000.0.47f1", "unwrap data field");

const noisy = parseCliJsonEnvelope(`info: connected\n${JSON.stringify({ success: true, data: { ok: true } })}`);
ok(unwrapCliData(noisy)?.ok === true, "parse JSON suffix from noisy stdout");

const prevCli = process.env.GAMEFORGE_UNITY_CLI;
const prevTest = process.env.GAMEFORGE_UNITY_TEST_VIA_CLI;
delete process.env.GAMEFORGE_UNITY_CLI;
delete process.env.GAMEFORGE_UNITY_TEST_VIA_CLI;
ok(isUnityCliIntegrationEnabled(), "CLI integration default on");
ok(preferUnityTestViaCli(), "test-via-cli default on");
process.env.GAMEFORGE_UNITY_CLI = "0";
ok(!isUnityCliIntegrationEnabled(), "GAMEFORGE_UNITY_CLI=0 disables integration");
process.env.GAMEFORGE_UNITY_TEST_VIA_CLI = "off";
ok(!preferUnityTestViaCli(), "GAMEFORGE_UNITY_TEST_VIA_CLI=off disables test path");
if (prevCli === undefined) delete process.env.GAMEFORGE_UNITY_CLI;
else process.env.GAMEFORGE_UNITY_CLI = prevCli;
if (prevTest === undefined) delete process.env.GAMEFORGE_UNITY_TEST_VIA_CLI;
else process.env.GAMEFORGE_UNITY_TEST_VIA_CLI = prevTest;

if (errors.length) {
  console.error(`\n${errors.length} failure(s)`);
  process.exit(1);
}
console.log("\nAll unity-cli smoke checks passed");
