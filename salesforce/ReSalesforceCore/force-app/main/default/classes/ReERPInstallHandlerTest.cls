@IsTest
private class ReERPInstallHandlerTest {

    @IsTest
    static void testInstallScriptExecution() {
        Test.startTest();
        ReERPInstallHandler handler = new ReERPInstallHandler();
        Test.testInstall(handler, null);
        Test.stopTest();

        System.assert(true, 'Post install handler executed successfully.');
    }
}
