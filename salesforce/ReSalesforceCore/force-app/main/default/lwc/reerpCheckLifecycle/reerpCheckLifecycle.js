import { LightningElement } from 'lwc';
import { ShowToastEvent } from 'lightning/platformShowToastEvent';

export default class ReerpCheckLifecycle extends LightningElement {
    handleEndorse() {
        this.dispatchEvent(new ShowToastEvent({ title: 'Bilgi', message: 'Çek Ciro Edildi.', variant: 'info' }));
    }
    handleCollect() {
        this.dispatchEvent(new ShowToastEvent({ title: 'Başarılı', message: 'Çek Tahsil Edildi.', variant: 'success' }));
    }
    handleBounce() {
        this.dispatchEvent(new ShowToastEvent({ title: 'Uyarı', message: 'Çek Karşılıksız İşlendi.', variant: 'warning' }));
    }
}
