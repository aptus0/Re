import { LightningElement, api, wire, track } from 'lwc';
import getAccount360Data from '@salesforce/apex/Account360Controller.getAccount360Data';

export default class Musteri360Dashboard extends LightningElement {
    @api recordId;
    @track account = {};

    @wire(getAccount360Data, { recordId: '$recordId' })
    wiredAccount({ error, data }) {
        if (data) {
            this.account = data;
        } else if (error) {
            console.error('Account 360 load error', error);
        }
    }
}
