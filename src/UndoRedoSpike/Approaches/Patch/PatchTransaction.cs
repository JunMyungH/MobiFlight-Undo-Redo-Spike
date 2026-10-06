public class PatchTransaction
{
    private readonly List<IPatchOperation> _operations;

    private readonly List<PatchTransaction>?
        _groupedTransactions;

    public IReadOnlyList<IPatchOperation> Operations =>
        _operations;

    public IReadOnlyList<PatchTransaction>?
        GroupedTransactions =>
            _groupedTransactions;

    public bool IsHistoryJump =>
        _groupedTransactions is not null;

    public string? Label { get; }

    public int ActionCount { get; }

    public PatchTransaction(
        IEnumerable<IPatchOperation> operations,
        string? label = null)
    {
        _operations =
            operations.ToList();

        Label =
            label;

        ActionCount = 1;
    }

    private PatchTransaction(
        IEnumerable<IPatchOperation> operations,
        string label,
        IEnumerable<PatchTransaction> groupedTransactions)
    {
        _operations =
            operations.ToList();

        Label =
            label;

        _groupedTransactions =
            groupedTransactions.ToList();

        ActionCount =
            _groupedTransactions.Sum(
                transaction =>
                    transaction.ActionCount);
    }

    public static PatchTransaction CreateHistoryJump(
        IEnumerable<PatchTransaction> transactions)
    {
        var transactionList =
            transactions.ToList();

        var actionCount =
            transactionList.Sum(
                transaction =>
                    transaction.ActionCount);

        return new PatchTransaction(
            transactionList.SelectMany(
                transaction =>
                    transaction.Operations),
            $"History Jump ({actionCount} actions)",
            transactionList);
    }

    public void Apply(
        ProjectState project)
    {
        var appliedOperations =
            new Stack<IPatchOperation>();

        try
        {
            foreach (
                var operation in
                _operations)
            {
                operation.Apply(
                    project);

                appliedOperations.Push(
                    operation);
            }
        }
        catch
        {
            while (
                appliedOperations.Count >
                0)
            {
                var operation =
                    appliedOperations.Pop();

                operation.Undo(
                    project);
            }

            throw;
        }
    }

    public void Undo(
        ProjectState project)
    {
        var undoneOperations =
            new Stack<IPatchOperation>();

        try
        {
            for (
                var i =
                    _operations.Count - 1;
                i >= 0;
                i--)
            {
                var operation =
                    _operations[i];

                operation.Undo(
                    project);

                undoneOperations.Push(
                    operation);
            }
        }
        catch
        {
            while (
                undoneOperations.Count >
                0)
            {
                var operation =
                    undoneOperations.Pop();

                operation.Apply(
                    project);
            }

            throw;
        }
    }
}