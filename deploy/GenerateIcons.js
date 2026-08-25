#!/usr/bin/env node
'use strict';

// Regenerates every logo asset derived from docs/roslynpad.svg:
//   docs/RoslynPad.png
//   deploy/resources/windows/PackageRoot/Assets/*.png
//   src/RoslynPad/Resources/RoslynPad.ico
//   src/RoslynPad/Resources/RoslynPad.icon/Assets/roslynpad-mac.svg
//
// The Icon Composer metadata (icon.json) is authored separately; the layer SVG it
// references is generated here as the logo's foreground shapes on their own.
//
// Two actool behaviours constrain that layer, and they are independent:
//
//   Curve flattening uses a tolerance in path user units, applied before any
//   transform. A small viewBox turns every curve into a visible polygon, and
//   wrapping the art in a scale() does not help - the coordinates have to be baked.
//   Hence docs/roslynpad.svg is authored in a 1024-unit viewBox.
//
//   The layer's intrinsic width/height maps onto a 1024pt canvas that spans the
//   whole 824pt tile, so the intrinsic size is what insets the artwork into the
//   icon grid: 1024 renders it edge-to-edge, 900 matches Icon Composer's preview.
//
// Usage: node deploy/GenerateIcons.js
//
// Requires Node 18+. @resvg/resvg-js is installed into deploy/.tools on first run.
// Each PNG is rendered straight at its target size: resvg's analytic anti-aliasing
// holds the thin counters in the C and # better than downsampling a larger render.

const { execFileSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

const RESVG_PACKAGE = '@resvg/resvg-js@^2.6.2';
const XMLDOM_PACKAGE = '@xmldom/xmldom@^0.8.11';
const ROOT = path.resolve(__dirname, '..');
const SVG = path.join(ROOT, 'docs', 'roslynpad.svg');
const ASSETS = path.join(__dirname, 'resources', 'windows', 'PackageRoot', 'Assets');
const ICO = path.join(ROOT, 'src', 'RoslynPad', 'Resources', 'RoslynPad.ico');
const MAC_SVG = path.join(
  ROOT, 'src', 'RoslynPad', 'Resources', 'RoslynPad.icon', 'Assets', 'roslynpad-mac.svg');

// Icon Composer draws the tile and the depth/specular pass itself, so the layer is
// just the shapes painted in the logo's foreground green - the white backing and
// its darker outline are dropped.
const MAC_LAYER_COLOR = '#388934';
const MAC_LAYER_SIZE = 900;
const MAC_LAYER_VIEWBOX = 1024;

// 256 is stored as an embedded PNG, the rest as BMP DIBs - the layout Windows expects.
const ICO_SIZES = [256, 64, 48, 32, 16];
const SCALES = [['100', 1], ['125', 1.25], ['150', 1.5], ['200', 2], ['400', 4]];

function scaled(prefix, base) {
  return SCALES.map(([suffix, factor]) =>
    [path.join(ASSETS, `${prefix}.scale-${suffix}.png`), Math.round(base * factor)]);
}

const PNG_TARGETS = [
  [path.join(ROOT, 'docs', 'RoslynPad.png'), 150],
  ...scaled('logo', 150),
  ...scaled('storelogo', 50),
  ...scaled('square44x44logo', 44),
  ...[16, 24, 32, 48, 256].map(size =>
    [path.join(ASSETS, `Square44x44Logo.altform-unplated_targetsize-${size}.png`), size]),
];

function resolvePackage(id) {
  for (const moduleId of [id, path.join(__dirname, '.tools', 'node_modules', id)]) {
    try {
      return require.resolve(moduleId);
    } catch (error) {
      if (error.code !== 'MODULE_NOT_FOUND') throw error;
    }
  }
  return null;
}

function loadTools() {
  let resvg = resolvePackage('@resvg/resvg-js');
  let xmldom = resolvePackage('@xmldom/xmldom');
  if (resvg && xmldom) return { resvg: require(resvg), xmldom: require(xmldom) };

  console.log(`Installing icon tooling into deploy/.tools ...`);
  execFileSync('npm', ['install', '--no-save', '--no-audit', '--no-fund',
    '--prefix', path.join(__dirname, '.tools'), RESVG_PACKAGE, XMLDOM_PACKAGE],
  { stdio: 'inherit' });

  resvg = path.join(__dirname, '.tools', 'node_modules', '@resvg', 'resvg-js');
  xmldom = path.join(__dirname, '.tools', 'node_modules', '@xmldom', 'xmldom');
  return { resvg: require(resvg), xmldom: require(xmldom) };
}

const tools = loadTools();
const { Resvg } = tools.resvg;
const { DOMParser, XMLSerializer } = tools.xmldom;
const svg = fs.readFileSync(SVG);

function createMacSvg(source) {
  const parseErrors = [];
  const document = new DOMParser({
    errorHandler: {
      warning: message => parseErrors.push(message),
      error: message => parseErrors.push(message),
      fatalError: message => parseErrors.push(message),
    },
  }).parseFromString(source.toString('utf8'), 'image/svg+xml');

  if (parseErrors.length > 0 || document.documentElement.localName !== 'svg') {
    throw new Error(`Failed to parse ${path.relative(ROOT, SVG)}: ${parseErrors.join('; ')}`);
  }

  const root = document.documentElement;
  root.setAttribute('width', String(MAC_LAYER_SIZE));
  root.setAttribute('height', String(MAC_LAYER_SIZE));

  const viewBox = (root.getAttribute('viewBox') || '').trim().split(/[\s,]+/).map(Number);
  if (viewBox.length !== 4 || viewBox.some(Number.isNaN) ||
      viewBox[2] !== MAC_LAYER_VIEWBOX || viewBox[3] !== MAC_LAYER_VIEWBOX) {
    throw new Error(`${path.relative(ROOT, SVG)} must use a "0 0 ${MAC_LAYER_VIEWBOX} ` +
      `${MAC_LAYER_VIEWBOX}" viewBox so actool flattens its curves finely.`);
  }

  const paint = value => (value || '').trim().toLowerCase();
  const isForeground = element =>
    paint(element.getAttribute('fill')) === MAC_LAYER_COLOR ||
    paint(element.getAttribute('stroke')) === MAC_LAYER_COLOR;

  // Keep a subtree if it is painted in the foreground colour, or still contains
  // something that is; drop everything else (the white backing, its outline, title).
  let keptElements = 0;
  const prune = parent => {
    let kept = 0;
    for (let child = parent.firstChild; child;) {
      const next = child.nextSibling;
      if (child.nodeType === 1) {
        if (isForeground(child)) {
          kept++;
          keptElements++;
        } else if (prune(child) > 0) {
          kept++;
        } else {
          parent.removeChild(child);
        }
      } else if (child.nodeType === 3 && child.data.trim() === '') {
        // keep the indentation
      } else {
        parent.removeChild(child);
      }
      child = next;
    }
    return kept;
  };
  prune(root);

  if (keptElements === 0) {
    throw new Error(
      `No ${MAC_LAYER_COLOR} elements found in ${path.relative(ROOT, SVG)}.`);
  }

  // Dropping elements leaves their indentation behind; collapse the blank lines.
  return `${new XMLSerializer().serializeToString(document).replace(/\n\s*(?=\n)/g, '')}\n`;
}

fs.writeFileSync(MAC_SVG, createMacSvg(svg));
console.log(path.relative(ROOT, MAC_SVG));

function render(size) {
  return new Resvg(svg, {
    fitTo: { mode: 'width', value: size },
    shapeRendering: 2, // geometricPrecision
    imageRendering: 0,
  }).render();
}

const CRC_TABLE = Array.from({ length: 256 }, (_, i) => {
  let c = i;
  for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  return c >>> 0;
});

function crc32(buffer) {
  let c = 0xffffffff;
  for (const byte of buffer) c = CRC_TABLE[(c ^ byte) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const length = Buffer.alloc(4);
  length.writeUInt32BE(data.length);
  const body = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(body));
  return Buffer.concat([length, body, crc]);
}

// resvg deflates at the default level; re-deflating its scanlines at level 9
// shaves ~20% off the committed files without touching a single pixel.
function recompress(png) {
  const chunks = [];
  const idat = [];

  for (let offset = 8; offset < png.length;) {
    const length = png.readUInt32BE(offset);
    const type = png.toString('ascii', offset + 4, offset + 8);
    const data = png.subarray(offset + 8, offset + 8 + length);
    (type === 'IDAT' ? idat : chunks).push(type === 'IDAT' ? data : { type, data });
    offset += 12 + length;
  }

  const deflated = zlib.deflateSync(zlib.inflateSync(Buffer.concat(idat)),
    { level: 9, memLevel: 9 });
  const end = chunks.pop(); // IEND
  return Buffer.concat([
    png.subarray(0, 8),
    ...chunks.map(c => chunk(c.type, c.data)),
    chunk('IDAT', deflated),
    chunk(end.type, end.data),
  ]);
}

function renderPng(size) {
  return recompress(Buffer.from(render(size).asPng()));
}

// A 32bpp bottom-up BGRA DIB with the doubled biHeight and (unused, all-zero)
// 1bpp AND mask that the ICO format still requires alongside the alpha channel.
// render().pixels is premultiplied - asPng() demultiplies on the way out, so the
// raw buffer has to be demultiplied here or antialiased edges pick up dark halos.
function bmpDib(size, pixels) {
  const header = Buffer.alloc(40);
  header.writeUInt32LE(40, 0);
  header.writeInt32LE(size, 4);
  header.writeInt32LE(size * 2, 8);
  header.writeUInt16LE(1, 12);
  header.writeUInt16LE(32, 14);
  header.writeUInt32LE(size * size * 4, 20);

  const bgra = Buffer.alloc(size * size * 4);
  const demultiply = (value, alpha) =>
    alpha === 0 ? 0 : Math.min(255, Math.round((value * 255) / alpha));

  for (let y = 0; y < size; y++) {
    const src = y * size * 4;
    const dst = (size - 1 - y) * size * 4;
    for (let x = 0; x < size * 4; x += 4) {
      const alpha = pixels[src + x + 3];
      bgra[dst + x] = demultiply(pixels[src + x + 2], alpha);
      bgra[dst + x + 1] = demultiply(pixels[src + x + 1], alpha);
      bgra[dst + x + 2] = demultiply(pixels[src + x], alpha);
      bgra[dst + x + 3] = alpha;
    }
  }

  const maskStride = Math.ceil(size / 32) * 4;
  return Buffer.concat([header, bgra, Buffer.alloc(maskStride * size)]);
}

function writeIco(file, sizes) {
  const images = sizes.map(size => {
    if (size >= 256) return renderPng(size);
    return bmpDib(size, render(size).pixels);
  });

  const directory = Buffer.alloc(6 + 16 * images.length);
  directory.writeUInt16LE(1, 2);
  directory.writeUInt16LE(images.length, 4);

  let offset = directory.length;
  images.forEach((image, i) => {
    const entry = 6 + 16 * i;
    directory.writeUInt8(sizes[i] & 0xff, entry); // 256 is encoded as 0
    directory.writeUInt8(sizes[i] & 0xff, entry + 1);
    directory.writeUInt16LE(1, entry + 4);
    directory.writeUInt16LE(32, entry + 6);
    directory.writeUInt32LE(image.length, entry + 8);
    directory.writeUInt32LE(offset, entry + 12);
    offset += image.length;
  });

  fs.writeFileSync(file, Buffer.concat([directory, ...images]));
  console.log(`${path.relative(ROOT, file).padEnd(72)} ${sizes.join('/')}`);
}

for (const [file, size] of PNG_TARGETS) {
  fs.writeFileSync(file, renderPng(size));
  console.log(`${path.relative(ROOT, file).padEnd(72)} ${size}x${size}`);
}

writeIco(ICO, ICO_SIZES);
