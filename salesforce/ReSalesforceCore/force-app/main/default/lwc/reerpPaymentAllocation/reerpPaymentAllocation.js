import { LightningElement } from 'lwc';
import { ShowToastEvent } from 'lightning/platformShowToastEvent';

export default class ReerpPaymentAllocation extends LightningElement {
    handleAutoAllocate() {
        this.dispatchEvent(new ShowToastEvent({
            title: 'Başarılı!',
            message: 'Açık fatura ve tahsilatlar başarıyla kapatıldı.',
            variant: 'success'
        }));
    }
}
