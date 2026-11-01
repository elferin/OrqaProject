using System;
using System.Threading;
using System.Threading.Tasks;
using Drone.Core.Authorization;
using Drone.Providers;
using NUnit.Framework;

namespace Drone.Tests {

    public class AuthorizationProviderTests {

        private const int WAIT_MILLISECONDS = 5000;

        private static readonly TimeSpan ShortDelay = TimeSpan.FromMilliseconds(10);
        private static readonly TimeSpan LongDelay = TimeSpan.FromMinutes(5);

        private static void WaitFor(Task task) {
            var finished = Task.WaitAny(new[] { task }, WAIT_MILLISECONDS) == 0;
            Assert.IsTrue(finished, "The task did not finish in time");
        }

        [Test]
        public void Guest_WithGate_IsAuthorizedImmediately() {
            using var gate = new AuthorizationGate();

            gate.Request(new GuestAuthorizationProvider());

            Assert.AreEqual(AuthState.Authorized, gate.State);
        }

        [Test]
        public void Remote_WhenLatencyPasses_ReturnsConfiguredResult() {
            var provider = new SimulatedRemoteAuthorizationProvider(ShortDelay, LongDelay, AuthResult.Denied("No licence"));

            var task = provider.AuthorizeAsync(CancellationToken.None);
            WaitFor(task);

            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);
            Assert.AreEqual("No licence", task.Result.Reason);
        }

        [Test]
        public void Remote_WhenCancelled_FinishesAsCancelled() {
            using var cts = new CancellationTokenSource();
            var provider = new SimulatedRemoteAuthorizationProvider(LongDelay, LongDelay, AuthResult.Success());

            var task = provider.AuthorizeAsync(cts.Token);
            cts.Cancel();
            WaitFor(task);

            Assert.AreEqual(TaskStatus.Canceled, task.Status);
        }

        [Test]
        public void Remote_WhenLatencyExceedsTimeout_Fails() {
            var provider = new SimulatedRemoteAuthorizationProvider(LongDelay, ShortDelay, AuthResult.Success());

            var task = provider.AuthorizeAsync(CancellationToken.None);
            WaitFor(task);

            Assert.AreEqual(TaskStatus.Faulted, task.Status);
            Assert.IsInstanceOf<TimeoutException>(task.Exception.GetBaseException());
        }

        [Test]
        public void Remote_WithGate_BecomesAuthorizedAfterPump() {
            using var gate = new AuthorizationGate();
            var provider = new SimulatedRemoteAuthorizationProvider(ShortDelay, LongDelay, AuthResult.Success());

            gate.Request(provider);
            Assert.AreEqual(AuthState.Authorizing, gate.State);

            var deadline = DateTime.UtcNow.AddMilliseconds(WAIT_MILLISECONDS);
            while (gate.State == AuthState.Authorizing && DateTime.UtcNow < deadline) {
                gate.Pump();
                Thread.Sleep(1);
            }

            Assert.AreEqual(AuthState.Authorized, gate.State);
        }
    }
}
