// Copia apenas os arquivos de distribuição necessários de node_modules para wwwroot/lib.
import { cpSync, mkdirSync, rmSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const modules = join(root, 'node_modules');
const lib = join(root, 'wwwroot', 'lib');

const files = {
    'bootstrap/css/bootstrap.min.css': 'bootstrap/dist/css/bootstrap.min.css',
    'bootstrap/js/bootstrap.bundle.min.js': 'bootstrap/dist/js/bootstrap.bundle.min.js',
    'bootstrap-icons/bootstrap-icons.min.css': 'bootstrap-icons/font/bootstrap-icons.min.css',
    'bootstrap-icons/fonts': 'bootstrap-icons/font/fonts',
    'datatables/dataTables.min.js': 'datatables.net/js/dataTables.min.js',
    'datatables/dataTables.bootstrap5.min.js': 'datatables.net-bs5/js/dataTables.bootstrap5.min.js',
    'datatables/dataTables.bootstrap5.min.css': 'datatables.net-bs5/css/dataTables.bootstrap5.min.css',
    'datatables/pt-BR.json': 'datatables.net-plugins/i18n/pt-BR.json',
    'fullcalendar/index.global.min.js': 'fullcalendar/index.global.min.js',
    'fullcalendar/pt-br.global.min.js': '@fullcalendar/core/locales/pt-br.global.min.js',
};

rmSync(lib, { recursive: true, force: true });
for (const [target, source] of Object.entries(files)) {
    const destination = join(lib, target);
    mkdirSync(dirname(destination), { recursive: true });
    cpSync(join(modules, source), destination, { recursive: true });
}
writeFileSync(join(lib, '.restored'), new Date().toISOString());
console.log(`Bibliotecas copiadas para ${lib}`);
