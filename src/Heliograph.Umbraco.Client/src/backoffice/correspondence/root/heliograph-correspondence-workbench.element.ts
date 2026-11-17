import { css, html } from 'lit';
import { customElement, state } from 'lit/decorators.js';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

// The workbench ships in Heliograph.Workbench, which the package references, and is served as a static file. It is
// the same element a site without Umbraco uses, so authors see one screen wherever Heliograph is hosted.
const WORKBENCH_SCRIPT = '/_content/Heliograph.Workbench/heliograph-workbench.js';
const API = '/heliograph/api';

interface WorkbenchModule {
  HttpWorkbenchSource: new (base: string, send: typeof fetch) => unknown;
}

/**
 * The Correspondence screen: Heliograph's workbench inside the backoffice. Each request carries the signed-in
 * user's token, fetched fresh every time because it can rotate, and the package's API checks they are in one of
 * the groups allowed to work on correspondence.
 */
@customElement('heliograph-correspondence-workbench')
export class HeliographCorrespondenceWorkbenchElement extends UmbLitElement {
  @state() private source: unknown = null;
  @state() private failed = '';

  override async connectedCallback() {
    super.connectedCallback();
    try {
      const auth = await this.getContext(UMB_AUTH_CONTEXT);
      const module = (await import(/* @vite-ignore */ WORKBENCH_SCRIPT)) as WorkbenchModule;
      const send: typeof fetch = async (input, init) => {
        const token = await auth?.getLatestToken();
        const headers = new Headers(init?.headers);
        if (token) headers.set('Authorization', `Bearer ${token}`);
        return fetch(input, { ...init, headers, credentials: 'same-origin' });
      };
      this.source = new module.HttpWorkbenchSource(API, send);
    } catch (error) {
      this.failed = `The workbench could not be loaded: ${(error as Error).message}`;
    }
  }

  override render() {
    if (this.failed) return html`<uui-box><p role="alert">${this.failed}</p></uui-box>`;
    if (!this.source) return html`<uui-loader></uui-loader>`;
    return html`<uui-box><heliograph-workbench .source=${this.source}></heliograph-workbench></uui-box>`;
  }

  static override styles = css`
    :host {
      display: block;
      padding: var(--uui-size-layout-1);
      /* The workbench follows the backoffice's own colours and type. */
      --heliograph-font: var(--uui-font-family);
      --heliograph-text: var(--uui-color-text);
      --heliograph-muted: var(--uui-color-text-alt);
      --heliograph-border: var(--uui-color-border);
      --heliograph-surface: var(--uui-color-surface);
      --heliograph-subtle: var(--uui-color-surface-alt);
      --heliograph-accent: var(--uui-color-interactive);
      --heliograph-error: var(--uui-color-danger);
      --heliograph-warning: var(--uui-color-warning-emphasis);
      --heliograph-ok: var(--uui-color-positive);
    }
  `;
}

export default HeliographCorrespondenceWorkbenchElement;

declare global {
  interface HTMLElementTagNameMap {
    'heliograph-correspondence-workbench': HeliographCorrespondenceWorkbenchElement;
  }
}
