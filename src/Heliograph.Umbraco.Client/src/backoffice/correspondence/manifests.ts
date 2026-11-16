// Every extension for the Correspondence screen, registered through one `type: "bundle"` entry in
// umbraco-package.json, as Wayfinder.Umbraco registers Blueprints. Each dynamic import becomes its own chunk.

import { manifests as rootManifests } from './root/manifests.js';

export const manifests = [...rootManifests];
