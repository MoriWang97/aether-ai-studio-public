const sharp = require('sharp');
const fs = require('fs');
const path = require('path');

const publicDir = path.join(__dirname, 'public');
const svgPath = path.join(publicDir, 'logo.svg');
const svgBuffer = fs.readFileSync(svgPath);

async function generateIcons() {
  console.log('Generating icons...');

  // Generate PNG icons
  await sharp(svgBuffer)
    .resize(192, 192)
    .png()
    .toFile(path.join(publicDir, 'logo192.png'));
  console.log('✓ logo192.png');

  await sharp(svgBuffer)
    .resize(512, 512)
    .png()
    .toFile(path.join(publicDir, 'logo512.png'));
  console.log('✓ logo512.png');

  // Generate favicon sizes
  const sizes = [16, 32, 48, 64, 128, 256];
  const pngFiles = [];
  
  for (const size of sizes) {
    const filename = `favicon-${size}.png`;
    await sharp(svgBuffer)
      .resize(size, size)
      .png()
      .toFile(path.join(publicDir, filename));
    pngFiles.push(path.join(publicDir, filename));
  }
  console.log('✓ favicon PNG sizes generated');

  // Generate ICO file from PNGs using dynamic import
  const pngToIco = (await import('png-to-ico')).default;
  const icoBuffer = await pngToIco(pngFiles);
  fs.writeFileSync(path.join(publicDir, 'favicon.ico'), icoBuffer);
  console.log('✓ favicon.ico');

  // Clean up temporary PNG files
  for (const file of pngFiles) {
    fs.unlinkSync(file);
  }
  console.log('✓ Cleaned up temporary files');

  console.log('\nDone! Icons generated successfully.');
}

generateIcons().catch(console.error);
