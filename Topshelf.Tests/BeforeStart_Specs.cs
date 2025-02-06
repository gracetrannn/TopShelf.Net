namespace Topshelf.Tests
{
    public class When_the_service_start_is_canceled
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Should_not_start_the_service()
        {
            bool started = false;

            var exitCode = HostFactory.Run(x =>
            {
                x.UseTestHost();

                x.Service(settings => new MyService(), s =>
                {
                    s.BeforeStartingService(hsc => hsc.CancelStart());
                    s.AfterStartingService(hsc => { started = true; });
                });
            });

            Assert.IsFalse(started);
            Assert.AreEqual(TopshelfExitCode.ServiceControlRequestFailed, exitCode);

            Assert.Pass();
        }

        class MyService : ServiceControl
        {
            public bool Start(HostControl hostControl)
            {
                return true;
            }

            public bool Stop(HostControl hostControl)
            {
                return true;
            }
        }
    }
}