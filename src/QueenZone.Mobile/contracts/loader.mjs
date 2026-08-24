import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const shimsDir = path.join(path.dirname(fileURLToPath(import.meta.url)), 'shims');

const shims = new Map([
  ['expo-constants', path.join(shimsDir, 'expo-constants.mjs')],
  ['react-native', path.join(shimsDir, 'react-native.mjs')],
  ['@react-native-async-storage/async-storage', path.join(shimsDir, 'async-storage.mjs')],
]);

export async function resolve(specifier, context, nextResolve) {
  const shim = shims.get(specifier);
  if (shim) {
    return { url: pathToFileURL(shim).href, shortCircuit: true };
  }

  if (specifier.startsWith('.') && context.parentURL) {
    try {
      return await nextResolve(specifier, context);
    } catch (err) {
      const parentDir = path.dirname(fileURLToPath(context.parentURL));
      const resolved = resolveRelative(parentDir, specifier);
      if (resolved) {
        return { url: pathToFileURL(resolved).href, shortCircuit: true };
      }
      throw err;
    }
  }

  return nextResolve(specifier, context);
}

export async function load(url, context, nextLoad) {
  const result = await nextLoad(url, context);
  if (!url.startsWith('file:') || !/\.tsx?$/i.test(fileURLToPath(url).replace(/\\/g, '/'))) {
    return result;
  }

  const source = decodeSource(result.source);
  if (!source || !/\brequire\s*\(/.test(source) || source.includes('const require = __createRequire')) {
    return result;
  }

  return {
    ...result,
    source: `import { createRequire as __createRequire } from 'node:module';\nconst require = __createRequire(${JSON.stringify(url)});\n${source}`,
  };
}

function resolveRelative(parentDir, specifier) {
  const extensions = ['.ts', '.tsx', '.js', '.mjs', '.cjs'];
  for (const ext of extensions) {
    const candidate = path.resolve(parentDir, specifier + ext);
    if (fs.existsSync(candidate) && fs.statSync(candidate).isFile()) {
      return candidate;
    }
  }

  const asDir = path.resolve(parentDir, specifier);
  if (fs.existsSync(asDir) && fs.statSync(asDir).isDirectory()) {
    for (const name of ['index.ts', 'index.tsx', 'index.js', 'index.mjs', 'index.cjs']) {
      const candidate = path.join(asDir, name);
      if (fs.existsSync(candidate) && fs.statSync(candidate).isFile()) {
        return candidate;
      }
    }
  }

  return null;
}

function decodeSource(source) {
  if (source == null) {
    return '';
  }
  if (typeof source === 'string') {
    return source;
  }
  if (Buffer.isBuffer(source)) {
    return source.toString('utf8');
  }
  if (source instanceof Uint8Array) {
    return Buffer.from(source).toString('utf8');
  }
  return String(source);
}
