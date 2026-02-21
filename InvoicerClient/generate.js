const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

// ── CONFIG ──────────────────────────────────────────────────────────────────
const IMAGE_TAG = process.env.OPENAPI_GEN_VERSION || 'v7.20.0';

const specUrl = 'http://host.docker.internal:5244/swagger/v1/swagger.json';
// On Linux: replace with e.g. 'http://172.17.0.1:5244/swagger/v1/swagger.json' or your host IP

const outputRelative = path.join('src', 'app', 'api');
const outputAbsolute = path.resolve(outputRelative);

const generator = 'typescript-angular';

const additionalProps = ['ngVersion=21.0.0', 'providedInRoot=true', 'serviceSuffix=Service'].join(
  ',',
);

const dockerImage = `openapitools/openapi-generator-cli:${IMAGE_TAG}`;

// ── FUNCTIONS ───────────────────────────────────────────────────────────────

function cleanGeneratedFolder() {
  if (fs.existsSync(outputAbsolute)) {
    console.log(`Cleaning existing folder: ${outputRelative}`);
    fs.rmSync(outputAbsolute, { recursive: true, force: true });
    console.log('→ Folder cleaned');
  } else {
    console.log(`No existing folder found at ${outputRelative} — nothing to clean`);
  }
}

function generateApiClient() {
  // Convert Windows backslashes to forward slashes for the container path
  const containerOutputPath = `/local/${outputRelative.replace(/\\/g, '/')}`;

  const dockerCommand = [
    'docker',
    'run',
    '--rm',
    '-v',
    `${process.cwd()}:/local`,
    dockerImage,
    'generate',
    '-i',
    specUrl,
    '-g',
    generator,
    '-o',
    containerOutputPath,
    '--additional-properties',
    additionalProps,
    // '--clean' exists but is for batch mode; manual rm above is more reliable for single generate
  ];

  console.log('Running OpenAPI Generator...');
  console.log('Command:', dockerCommand.join(' '));
  console.log('');

  execSync(dockerCommand.join(' '), { stdio: 'inherit' });
  console.log(`Generation complete → files written to ./${outputRelative}`);
}

function formatGeneratedCode() {
  console.log('Formatting generated code with Prettier...');

  // Scope prettier only to the generated folder — safer & faster
  const prettierCmd = `npx prettier "${outputRelative}/**/*.{ts,js,json,md}" --write`;

  try {
    execSync(prettierCmd, { stdio: 'inherit' });
    console.log('→ Formatting done');
  } catch (err) {
    console.warn('Prettier failed or not installed — skipping formatting');
    console.warn('(Make sure prettier is in devDependencies)');
  }
}

// ── MAIN EXECUTION ──────────────────────────────────────────────────────────
console.log(`API Client Generator (OpenAPI Generator ${IMAGE_TAG} via Docker)`);
console.log('Spec:     ' + specUrl);
console.log('Output:   ' + outputRelative);
console.log('');

try {
  cleanGeneratedFolder();
  generateApiClient();
  formatGeneratedCode();

  console.log('\nAll steps completed successfully!');
  console.log('Remember to restart your Angular dev server if it was watching.');
} catch (error) {
  console.error('\nERROR during generation:');
  console.error(error.message || error);
  console.log('\nQuick troubleshooting:');
  console.log('• Docker running?');
  console.log('• .NET API running and accessible at port 5244?');
  console.log('• On Linux? Update host.docker.internal → real host IP');
  console.log('• Image available? Try: docker pull ' + dockerImage);
  process.exit(1);
}
