namespace QueenZone.Data;

public sealed class SharedSearchReindexRunRequestStore
{
    private readonly object gate = new();
    private readonly List<SearchReindexRunRequest> requests = [];
    private long nextId = 1;

    internal SearchReindexRunRequestQueueResult Queue(SearchReindexRunRequestCreate create)
    {
        ArgumentNullException.ThrowIfNull(create);

        lock (gate)
        {
            var active = requests.LastOrDefault(request =>
                request.Status is SearchReindexRunRequestStatus.Pending or SearchReindexRunRequestStatus.Running);
            if (active is not null)
            {
                return new SearchReindexRunRequestQueueResult(active, WasCreated: false);
            }

            var request = new SearchReindexRunRequest(
                nextId++,
                SearchReindexRunRequestStatus.Pending,
                create.RequestedBy,
                DateTime.UtcNow,
                RunnerId: null,
                StartedAtUtc: null,
                CompletedAtUtc: null,
                Summary: null,
                ErrorMessage: null);
            requests.Add(request);
            return new SearchReindexRunRequestQueueResult(request, WasCreated: true);
        }
    }

    internal SearchReindexRunRequest? ClaimNext(string runnerId)
    {
        lock (gate)
        {
            var staleBefore = DateTime.UtcNow.AddHours(-3);
            for (var requestIndex = 0; requestIndex < requests.Count; requestIndex++)
            {
                var request = requests[requestIndex];
                if (request.Status == SearchReindexRunRequestStatus.Running
                    && request.StartedAtUtc < staleBefore)
                {
                    requests[requestIndex] = request with
                    {
                        Status = SearchReindexRunRequestStatus.Pending,
                        RunnerId = null,
                        StartedAtUtc = null
                    };
                }
            }

            var index = requests.FindIndex(request => request.Status == SearchReindexRunRequestStatus.Pending);
            if (index < 0)
            {
                return null;
            }

            var claimed = requests[index] with
            {
                Status = SearchReindexRunRequestStatus.Running,
                RunnerId = runnerId,
                StartedAtUtc = DateTime.UtcNow
            };
            requests[index] = claimed;
            return claimed;
        }
    }

    internal bool Complete(long requestId, string summary) =>
        UpdateRunning(requestId, request => request with
        {
            Status = SearchReindexRunRequestStatus.Completed,
            CompletedAtUtc = DateTime.UtcNow,
            Summary = summary,
            ErrorMessage = null
        });

    internal bool Fail(long requestId, string errorMessage) =>
        UpdateRunning(requestId, request => request with
        {
            Status = SearchReindexRunRequestStatus.Failed,
            CompletedAtUtc = DateTime.UtcNow,
            ErrorMessage = errorMessage
        });

    internal bool ReturnToPending(long requestId) =>
        UpdateRunning(requestId, request => request with
        {
            Status = SearchReindexRunRequestStatus.Pending,
            RunnerId = null,
            StartedAtUtc = null
        });

    internal IReadOnlyList<SearchReindexRunRequest> ListRecent(int limit)
    {
        lock (gate)
        {
            return requests
                .OrderByDescending(request => request.RequestedAtUtc)
                .Take(limit)
                .ToList();
        }
    }

    private bool UpdateRunning(
        long requestId,
        Func<SearchReindexRunRequest, SearchReindexRunRequest> update)
    {
        lock (gate)
        {
            var index = requests.FindIndex(request =>
                request.Id == requestId && request.Status == SearchReindexRunRequestStatus.Running);
            if (index < 0)
            {
                return false;
            }

            requests[index] = update(requests[index]);
            return true;
        }
    }
}
