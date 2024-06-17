using Iceshrimp.Backend.Core.Helpers;

namespace Iceshrimp.Tests.Concurrency;

[TestClass]
public class EventTests
{
	[TestMethod]
	public async Task TestAsyncAutoResetEvent()
	{
		var autoResetEvent = new AsyncAutoResetEvent();
		var pre            = DateTime.Now;
		var task           = autoResetEvent.WaitAsync();
		_ = Task.Run(async () =>
		{
			await Task.Delay(50);
			autoResetEvent.Set();
		});
		await task;
		(DateTime.Now - pre).Should().BeGreaterThan(TimeSpan.FromMilliseconds(45));
		autoResetEvent.Signaled.Should().BeFalse();
	}

	[TestMethod]
	public async Task TestAsyncAutoResetEventWithoutReset()
	{
		var autoResetEvent = new AsyncAutoResetEvent();
		var pre            = DateTime.Now;
		var task           = autoResetEvent.WaitWithoutResetAsync();
		_ = Task.Run(async () =>
		{
			await Task.Delay(50);
			autoResetEvent.Set();
		});
		await task;
		(DateTime.Now - pre).Should().BeGreaterThan(TimeSpan.FromMilliseconds(45));
		autoResetEvent.Signaled.Should().BeTrue();
	}

	[TestMethod]
	public async Task TestAsyncAutoResetEventMulti()
	{
		var    autoResetEvent = new AsyncAutoResetEvent();
		var    pre            = DateTime.Now;
		Task[] tasks          = [autoResetEvent.WaitAsync(), autoResetEvent.WaitAsync()];
		_ = Task.Run(async () =>
		{
			await Task.Delay(50);
			autoResetEvent.Set();
		});
		await Task.WhenAll(tasks);
		(DateTime.Now - pre).Should().BeGreaterThan(TimeSpan.FromMilliseconds(45));
		autoResetEvent.Signaled.Should().BeFalse();
	}

	[TestMethod]
	public async Task TestAsyncAutoResetEventWithoutResetMulti()
	{
		var    autoResetEvent = new AsyncAutoResetEvent();
		var    pre            = DateTime.Now;
		Task[] tasks          = [autoResetEvent.WaitWithoutResetAsync(), autoResetEvent.WaitWithoutResetAsync()];
		_ = Task.Run(async () =>
		{
			await Task.Delay(50);
			autoResetEvent.Set();
		});
		await Task.WhenAll(tasks);
		(DateTime.Now - pre).Should().BeGreaterThan(TimeSpan.FromMilliseconds(45));
		autoResetEvent.Signaled.Should().BeTrue();
	}

	[TestMethod]
	public async Task TestAsyncAutoResetEventPre()
	{
		var autoResetEvent = new AsyncAutoResetEvent();
		autoResetEvent.Set();
		await autoResetEvent.WaitAsync();
		autoResetEvent.Signaled.Should().BeFalse();
		await Assert.ThrowsExceptionAsync<TimeoutException>(() => autoResetEvent.WaitAsync()
			                                                    .WaitAsync(TimeSpan.FromMilliseconds(45)));
	}

	[TestMethod]
	public async Task TestAsyncAutoResetEventPreMulti()
	{
		var autoResetEvent = new AsyncAutoResetEvent();
		autoResetEvent.Set();
		await autoResetEvent.WaitWithoutResetAsync();
		autoResetEvent.Signaled.Should().BeTrue();
		await autoResetEvent.WaitWithoutResetAsync();
		autoResetEvent.Signaled.Should().BeTrue();
	}
}