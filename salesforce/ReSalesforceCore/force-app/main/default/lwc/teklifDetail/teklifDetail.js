import { LightningElement, api, wire, track } from 'lwc';
import getProposalDetail from '@salesforce/apex/ProposalController.getProposalDetail';
import syncProposalToErp from '@salesforce/apex/ErpIntegrationService.syncProposalToErp';
import { ShowToastEvent } from 'lightning/platformShowToastEvent';

export default class TeklifDetail extends LightningElement {
    @api recordId;
    @track proposal = {};

    @wire(getProposalDetail, { recordId: '$recordId' })
    wiredProposal({ error, data }) {
        if (data) {
            this.proposal = data;
        } else if (error) {
            console.error('Proposal data load error', error);
        }
    }

    get lineItemsCount() {
        return this.proposal && this.proposal.lineItems ? this.proposal.lineItems.length : 0;
    }

    handleOpenErp() {
        syncProposalToErp({ proposalId: this.recordId })
            .then((result) => {
                this.dispatchEvent(
                    new ShowToastEvent({
                        title: result.success ? 'ERP senkronizasyonu tamamlandı' : 'ERP hatası',
                        message: result.message,
                        variant: result.success ? 'success' : 'error'
                    })
                );
            })
            .catch((error) => {
                this.dispatchEvent(
                    new ShowToastEvent({
                        title: 'ERP senkronizasyonu başarısız',
                        message: error?.body?.message || error.message,
                        variant: 'error'
                    })
                );
            });
    }
}
