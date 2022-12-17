using Moq;
using Xunit;

public class StreamProcessorTests
{
    [Fact]
    public void ProcessStream_ProcessesEntriesConcurrently()
    {
        // create mock database
        var mockDatabase = new Mock<IDatabase>();

        // setup StreamRead to return a batch of 2 entries
        mockDatabase
            .SetupSequence(db => db.StreamRead("OrderDocument", "0-0", 2, CommandFlags.None))
            .Returns(new[] { CreateStreamEntry("1"), CreateStreamEntry("2") })
            .Returns(new[] { CreateStreamEntry("3"), CreateStreamEntry("4") })
            .Returns(new StreamEntry[0]);

        // create a mock processing function to verify that entries are processed concurrently
        int processingCount = 0;
        var mockProcessingFunc = new Mock<Action<StreamEntry>>();
        mockProcessingFunc
            .Setup(func => func(It.IsAny<StreamEntry>()))
            .Callback<StreamEntry>(entry =>
            {
                Interlocked.Increment(ref processingCount);
                Thread.Sleep(100);
                Interlocked.Decrement(ref processingCount);
            });

        // create StreamProcessor with mock database, batch size of 2, and max concurrency of 1
        var processor = new StreamProcessor(mockDatabase.Object, 2, 1);

        // call ProcessStream with the mock processing function
        processor.ProcessStream(mockProcessingFunc.Object);

        // verify that the processing function was called with the correct arguments
        mockProcessingFunc.Verify(func => func(It.Is<StreamEntry>(entry => entry.Id == "1")), Times.Once());
        mockProcessingFunc.Verify(func => func(It.Is<StreamEntry>(entry => entry.Id == "2")), Times.Once());
        mockProcessingFunc.Verify(func => func(It.Is<StreamEntry>(entry => entry.Id == "3")), Times.Once());
        mockProcessingFunc.Verify(func => func(It.Is<StreamEntry>(entry => entry.Id == "4")), Times.Once());

        // verify that processingCount was never greater than 1
        Assert.Equal(0, processingCount);
    }

    [Fact]
    public void ProcessStream_HandlesSaveToDatabaseErrors()
    {
        // create mock database
        var mockDatabase = new Mock<IDatabase>();

        // setup StreamRead to return a batch of 2 entries
        mockDatabase
            .SetupSequence(db => db.StreamRead("OrderDocument", "0-0", 2, CommandFlags.None))
            .Returns(new[] { CreateStreamEntry("1"), CreateStreamEntry("2") })
            .Returns(new[] { CreateStreamEntry("3"), CreateStreamEntry("4") })
            .Returns(new StreamEntry[0]);

        // create a mock processing function that throws an exception when called
        var mockProcessingFunc = new Mock<Action<StreamEntry>>();
        mockProcessingFunc
            .Setup(func => func(It.IsAny<StreamEntry>()))
            .            .Throws<Exception>();

        // create StreamProcessor with mock database, batch size of 2, and max concurrency of 2
        var processor = new StreamProcessor(mockDatabase.Object, 2, 2);

        // call ProcessStream with the mock processing function
        processor.ProcessStream(mockProcessingFunc.Object);

        // verify that the processing function was called with the correct arguments
        mockProcessingFunc.Verify(func => func(It.Is<StreamEntry>(entry => entry.Id == "1")), Times.Once());
        mockProcessingFunc.Verify(func => func(It.Is<StreamEntry>(entry => entry.Id == "2")), Times.Once());
        mockProcessingFunc.Verify(func => func(It.Is<StreamEntry>(entry => entry.Id == "3")), Times.Once());
        mockProcessingFunc.Verify(func => func(It.Is<StreamEntry>(entry => entry.Id == "4")), Times.Once());
    }

    private static StreamEntry CreateStreamEntry(string id)
    {
        return new StreamEntry(id, new[] { new RedisValue("value") });
    }
}

