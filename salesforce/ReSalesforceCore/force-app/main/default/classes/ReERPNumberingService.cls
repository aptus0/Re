public with sharing class ReERPNumberingService {

    public static String generateNumber(Id companyId, String documentType) {
        String yearStr = String.valueOf(Date.today().year());
        String prefix = documentType == 'SALES_INVOICE' ? 'SI' : 'EV';
        Long timestamp = System.currentTimeMillis();
        String seq = String.valueOf(timestamp).right(6);
        return prefix + yearStr + seq;
    }
}
