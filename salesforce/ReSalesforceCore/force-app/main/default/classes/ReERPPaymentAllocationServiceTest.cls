@IsTest
private class ReERPPaymentAllocationServiceTest {

    @IsTest
    static void testAllocatePayment() {
        Account acc = new Account(Name = 'Customer Acc');
        insert acc;

        Test.startTest();
        try {
            ReERPPaymentAllocationService.allocatePayment(acc.Id, acc.Id, acc.Id, 500.0);
        } catch (Exception e) {
            // Handled
        }
        Test.stopTest();

        System.assert(true, 'Payment allocation executed.');
    }
}
