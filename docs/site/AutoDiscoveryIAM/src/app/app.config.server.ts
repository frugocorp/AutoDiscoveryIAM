import { mergeApplicationConfig, ApplicationConfig } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';
import { CONTENT_DISK_READER } from '@frugocorp/create-ngx-docs-site/services/content/content.service';
import { readContentFromDisk } from '@frugocorp/create-ngx-docs-site/services/content/content-disk-reader.server';
import { FILE_EMBED_DISK_READER } from '@ngx-docs-markdown-kit/parser-md-file-embed';
import { readFileEmbedFromDisk } from '@frugocorp/create-ngx-docs-site/services/content/file-embed-disk-reader.server';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';

const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(withRoutes(serverRoutes)),
    { provide: CONTENT_DISK_READER, useValue: readContentFromDisk },
    { provide: FILE_EMBED_DISK_READER, useValue: readFileEmbedFromDisk },
  ]
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
