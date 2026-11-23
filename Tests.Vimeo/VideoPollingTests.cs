using Apps.Vimeo.Models.Responses;
using Apps.Vimeo.Polling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Vimeo;

[TestClass]
public class VideoPollingTests
{
    private static readonly DateTime StartedAt = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void FirstPollEstablishesBaselineAndIgnoresHistory()
    {
        var result = VideoPolling.BuildResponse(null, StartedAt, [Video("123", StartedAt.AddMinutes(-1))]);

        Assert.IsFalse(result.FlyBird);
        Assert.IsNull(result.Result);
        Assert.AreEqual(StartedAt, result.Memory!.StartedAt);
        Assert.AreEqual(StartedAt, result.Memory.ScannedThrough);
    }

    [TestMethod]
    public void OverlapFindsLateArrivalsWithoutEmittingDuplicates()
    {
        var baseline = VideoPolling.BuildResponse(null, StartedAt, []).Memory;
        var firstVideo = Video("124", StartedAt.AddMinutes(1));
        var delayedVideo = Video("125", StartedAt.AddSeconds(30));

        var first = VideoPolling.BuildResponse(baseline, StartedAt.AddMinutes(2), [firstVideo, firstVideo]);
        var second = VideoPolling.BuildResponse(first.Memory, StartedAt.AddMinutes(3), [firstVideo, delayedVideo]);
        var third = VideoPolling.BuildResponse(second.Memory, StartedAt.AddMinutes(4), [firstVideo, delayedVideo]);

        CollectionAssert.AreEqual(new[] { "124" }, first.Result!.Select(video => video.VideoId).ToArray());
        CollectionAssert.AreEqual(new[] { "125" }, second.Result!.Select(video => video.VideoId).ToArray());
        Assert.IsFalse(third.FlyBird);
        Assert.IsNull(third.Result);
        Assert.AreEqual(StartedAt.AddMinutes(4), third.Memory!.ScannedThrough);
    }

    [TestMethod]
    public void MoreThanOneHundredVideosAtTheSameInstantDrainAcrossPolls()
    {
        var baseline = VideoPolling.BuildResponse(null, StartedAt, []).Memory;
        var videos = Enumerable.Range(1000, 240)
            .Select(id => Video(id.ToString(), StartedAt.AddMinutes(1))).Reverse().ToList();

        var first = VideoPolling.BuildResponse(baseline, StartedAt.AddMinutes(10), videos);
        var second = VideoPolling.BuildResponse(first.Memory, StartedAt.AddMinutes(11), videos);
        var third = VideoPolling.BuildResponse(second.Memory, StartedAt.AddMinutes(12), videos);
        var ids = first.Result!.Concat(second.Result!).Concat(third.Result!).Select(video => video.VideoId).ToList();

        Assert.AreEqual(100, first.Result!.Count);
        Assert.AreEqual(100, second.Result!.Count);
        Assert.AreEqual(40, third.Result!.Count);
        CollectionAssert.AreEquivalent(videos.Select(video => video.VideoId).ToList(), ids);
    }

    [TestMethod]
    public void PriorVideosAndFutureTimestampsAreExcluded()
    {
        var baseline = VideoPolling.BuildResponse(null, StartedAt, []).Memory;
        var now = StartedAt.AddMinutes(1);
        var result = VideoPolling.BuildResponse(baseline, now,
        [
            Video("1", StartedAt.AddTicks(-1)),
            Video("2", now),
            Video("3", now.AddTicks(1))
        ]);

        CollectionAssert.AreEqual(new[] { "2" }, result.Result!.Select(video => video.VideoId).ToArray());
    }

    [TestMethod]
    public void CatchingUpOnLateArrivalsDoesNotReopenAnExpiredWindow()
    {
        var baseline = VideoPolling.BuildResponse(null, StartedAt, []).Memory;
        var old = Video("12", StartedAt.AddMinutes(3));
        var initial = VideoPolling.BuildResponse(baseline, StartedAt.AddMinutes(4), [old]);
        var later = VideoPolling.BuildResponse(initial.Memory, StartedAt.AddMinutes(10), [old]);
        var delayed = Enumerable.Range(100, 105).Select(id => Video(id.ToString(), StartedAt.AddMinutes(6))).ToList();

        var first = VideoPolling.BuildResponse(later.Memory, StartedAt.AddMinutes(11), delayed);
        var next = VideoPolling.BuildResponse(first.Memory, StartedAt.AddMinutes(12), delayed.Append(old));

        Assert.AreEqual(100, first.Result!.Count);
        Assert.AreEqual(5, next.Result!.Count);
        Assert.IsFalse(next.Result.Any(video => video.VideoId == old.VideoId));
    }

    private static VideoResponse Video(string id, DateTime time) => new() { VideoId = id, CreatedAt = time };
}
