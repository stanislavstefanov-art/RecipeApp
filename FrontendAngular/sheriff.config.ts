import { SheriffConfig } from '@softarc/sheriff-core';

/**
 * Architecture boundaries for FrontendAngular.
 *
 * Layers (coarse to fine):
 *   - core     : app-wide singletons (interceptors, error handlers, config)
 *   - ui       : reusable presentational components; no service injection
 *   - feature  : feature-shell + smart components + feature signal stores
 *   - data     : typed API clients and DTOs (lives under app/api/)
 *
 * Dependency rules (what a layer is allowed to import from):
 *   - feature  -> ui, core, data
 *   - ui       -> ui            (no core, no feature, no data)
 *   - core     -> core, data
 *   - data     -> data
 *
 * This config is permissive today (only the sample component exists). The
 * intent is to catch violations as soon as the first feature slice lands.
 */
export const config: SheriffConfig = {
  autoTagging: false,
  modules: {
    'src/app/core': 'type:core',
    'src/app/shared/ui': 'type:ui',
    'src/app/api': 'type:data',
    'src/app/features/<feature>': ['type:feature', 'scope:<feature>'],
  },
  depRules: {
    root: ['type:*'],
    'type:feature': ['type:ui', 'type:core', 'type:data'],
    'type:ui': 'type:ui',
    'type:core': ['type:core', 'type:data'],
    'type:data': 'type:data',
    'scope:*': ['scope:shared', 'sameTag'],
  },
};
