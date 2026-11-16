import { css, html } from 'lit';
import { customElement } from 'lit/decorators.js';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';

/**
 * What the Correspondence screen shows until the list of correspondence is built: what Heliograph is for, in the
 * author's words, and that nothing can be uploaded here yet. It says so plainly so nobody hunts for a button.
 */
@customElement('heliograph-correspondence-overview')
export class HeliographCorrespondenceOverviewElement extends UmbLitElement {
  override render() {
    return html`
      <uui-box headline="Letters, emails and texts, written in your own word processor">
        <p>
          Heliograph checks each piece of correspondence against a dictionary of the data it may use, shows you
          what is wrong and where, and renders it for each person it is sent to.
        </p>
        <p data-heliograph-status="not-built">
          <strong>You cannot upload correspondence here yet.</strong> This screen arrives with the next release of
          Heliograph.Umbraco.
        </p>
      </uui-box>
    `;
  }

  static override styles = css`
    :host {
      display: block;
      padding: var(--uui-size-layout-1);
    }
    p {
      max-width: 60ch;
    }
  `;
}

export default HeliographCorrespondenceOverviewElement;

declare global {
  interface HTMLElementTagNameMap {
    'heliograph-correspondence-overview': HeliographCorrespondenceOverviewElement;
  }
}
