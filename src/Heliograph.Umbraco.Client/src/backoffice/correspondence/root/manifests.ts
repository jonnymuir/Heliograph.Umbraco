// Mounts Correspondence into the Settings section's Advanced menu, the way Wayfinder.Umbraco mounts Blueprints:
// a menuItem that opens a root workspace. Reaching the screen through a real menu item is what gives it an entity
// context before anything renders.

import { HELIOGRAPH_CORRESPONDENCE_ROOT_ENTITY_TYPE, HELIOGRAPH_CORRESPONDENCE_ROOT_WORKSPACE_ALIAS } from '../entity.js';

export const manifests = [
  {
    type: 'workspace',
    kind: 'default',
    alias: HELIOGRAPH_CORRESPONDENCE_ROOT_WORKSPACE_ALIAS,
    name: 'Correspondence Root Workspace',
    meta: {
      entityType: HELIOGRAPH_CORRESPONDENCE_ROOT_ENTITY_TYPE,
      headline: 'Correspondence',
    },
  },
  {
    type: 'workspaceView',
    alias: 'Heliograph.WorkspaceView.CorrespondenceRoot.Overview',
    name: 'Correspondence Root Overview Workspace View',
    element: () => import('./heliograph-correspondence-overview.element.js'),
    weight: 100,
    meta: {
      label: 'Correspondence',
      pathname: 'overview',
      icon: 'icon-message',
    },
    conditions: [
      {
        alias: 'Umb.Condition.WorkspaceAlias',
        match: HELIOGRAPH_CORRESPONDENCE_ROOT_WORKSPACE_ALIAS,
      },
    ],
  },
  {
    type: 'menuItem',
    alias: 'Heliograph.MenuItem.CorrespondenceRoot',
    name: 'Correspondence Menu Item',
    // Blueprints is 100. A lower weight sorts later, so Correspondence sits just below it.
    weight: 90,
    meta: {
      label: 'Correspondence',
      icon: 'icon-message',
      entityType: HELIOGRAPH_CORRESPONDENCE_ROOT_ENTITY_TYPE,
      menus: ['Umb.Menu.AdvancedSettings'],
    },
  },
];
