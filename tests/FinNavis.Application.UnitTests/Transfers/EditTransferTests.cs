using FinNavis.Application.Transfers;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Transfers;

public sealed class EditTransferTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly OccurredOn = new(2026, 10, 29);

    private readonly FakeTransferRepository _transfers = new();
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeUnitOfWork _unitOfWork;

    public EditTransferTests() => _unitOfWork = new FakeUnitOfWork(_transfers.Commit);

    // The entity has no category field at all, so the attempt is refused with the sentence the
    // owner reads rather than being dropped on the way in.
    [Fact]
    public async Task EditTransfer_GivenACategory_IsRefused()
    {
        Transfer transfer = SeedTransfer(200.0000m);
        Guid categoryId = Guid.CreateVersion7();

        Func<Task> edit = () => EditAsync(transfer.Id, new EditTransferChanges(CategoryId: categoryId));

        var refusal = (await edit.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Transfer.CategoryNotAllowed");
        refusal.Message.Should().Be("Transfers carry no category.");
        _transfers.Committed(transfer.Id).Amount.Should().Be(200.0000m);
        _unitOfWork.Commits.Should().Be(0);
    }

    // The row itself changes, with no reversing entry beside it.
    [Fact]
    public async Task EditTransfer_NewAmount_AppliedInPlace()
    {
        Transfer transfer = SeedTransfer(200.0000m);

        Transfer edited = await EditAsync(transfer.Id, new EditTransferChanges(Amount: 180.0000m));

        edited.Id.Should().Be(transfer.Id);
        edited.Amount.Should().Be(new Money(180.0000m));
        _transfers.Stored.Should().ContainSingle();
        _transfers.Committed(transfer.Id).Amount.Should().Be(180.0000m);
        _unitOfWork.Commits.Should().Be(1);
    }

    // A field the owner left out keeps the value the transfer already carries.
    [Fact]
    public async Task EditTransfer_OnlyTheDate_LeavesEveryOtherFieldAsItWas()
    {
        Transfer transfer = SeedTransfer(200.0000m);
        var movedTo = new DateOnly(2026, 10, 12);

        Transfer edited = await EditAsync(transfer.Id, new EditTransferChanges(OccurredOn: movedTo));

        edited.OccurredOn.Should().Be(movedTo);
        edited.Amount.Should().Be(new Money(200.0000m));
        edited.SourceAccountId.Should().Be(transfer.SourceAccountId);
        edited.DestinationAccountId.Should().Be(transfer.DestinationAccountId);
    }

    [Fact]
    public async Task EditTransfer_NewDestination_MovesThatSideOnly()
    {
        Transfer transfer = SeedTransfer(200.0000m);
        Account holiday = SeedAccount("Holiday fund");

        Transfer edited = await EditAsync(
            transfer.Id,
            new EditTransferChanges(DestinationAccountId: holiday.Id));

        edited.DestinationAccountId.Should().Be(holiday.Id);
        edited.SourceAccountId.Should().Be(transfer.SourceAccountId);
    }

    // On the edit path the entity refuses before it assigns the first field.
    [Fact]
    public async Task EditTransfer_IntoTheSourceAccount_IsRefusedAndChangesNothing()
    {
        Transfer transfer = SeedTransfer(200.0000m);

        Func<Task> edit = () => EditAsync(
            transfer.Id,
            new EditTransferChanges(DestinationAccountId: transfer.SourceAccountId));

        (await edit.Should().ThrowAsync<DomainRuleViolationException>())
            .Which.Code.Should().Be("Transfer.SameAccount");

        _transfers.Committed(transfer.Id).DestinationAccountId
            .Should().Be(transfer.DestinationAccountId);
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task EditTransfer_UnknownSourceAccount_IsRefusedAndChangesNothing()
    {
        Transfer transfer = SeedTransfer(200.0000m);
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> edit = () => EditAsync(transfer.Id, new EditTransferChanges(SourceAccountId: unknownId));

        var refusal = (await edit.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Account.SourceNotFound");
        refusal.Message.Should().Be($"No account in the ledger has id {unknownId}.");
        _transfers.Committed(transfer.Id).SourceAccountId.Should().Be(transfer.SourceAccountId);
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task EditTransfer_UnknownDestinationAccount_IsRefusedAndChangesNothing()
    {
        Transfer transfer = SeedTransfer(200.0000m);
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> edit = () => EditAsync(
            transfer.Id,
            new EditTransferChanges(DestinationAccountId: unknownId));

        (await edit.Should().ThrowAsync<DomainRuleViolationException>())
            .Which.Code.Should().Be("Account.DestinationNotFound");

        _transfers.Committed(transfer.Id).DestinationAccountId
            .Should().Be(transfer.DestinationAccountId);
    }

    [Fact]
    public async Task EditTransfer_UnknownTransferId_IsRefusedAsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> edit = () => EditAsync(unknownId, new EditTransferChanges(Amount: 1.0000m));

        var refusal = (await edit.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Transfer.NotFound");
        refusal.Message.Should().Be($"No transfer in the ledger has id {unknownId}.");
        _unitOfWork.Commits.Should().Be(0);
    }

    // On the edit path too: one row, so a failed commit moved neither account.
    [Fact]
    public async Task EditTransfer_CommitFails_ReportsTheRecordingFailedAndChangesNothing()
    {
        Transfer transfer = SeedTransfer(200.0000m);
        var failingUnitOfWork = new FakeUnitOfWork(() => throw new InvalidOperationException("commit failed"));

        Func<Task> edit = () => new EditTransfer(_transfers, _accounts, failingUnitOfWork)
            .ExecuteAsync(transfer.Id, new EditTransferChanges(Amount: 180.0000m), CancellationToken.None);

        var refusal = (await edit.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Transfer.NotRecorded");
        refusal.InnerException.Should().BeOfType<InvalidOperationException>();
        _transfers.Committed(transfer.Id).Amount.Should().Be(200.0000m);
    }

    [Fact]
    public async Task EditTransfer_WhenCalled_PassesTheCancellationTokenThrough()
    {
        Transfer transfer = SeedTransfer(200.0000m);
        using var cancellation = new CancellationTokenSource();

        await new EditTransfer(_transfers, _accounts, _unitOfWork)
            .ExecuteAsync(transfer.Id, new EditTransferChanges(Amount: 180.0000m), cancellation.Token);

        _transfers.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _unitOfWork.TokensSeen.Should().AllSatisfy(token => token.Should().Be(cancellation.Token));
    }

    private Account SeedAccount(string name)
    {
        Account account = Account.Create(Guid.CreateVersion7(), name, Now);
        _accounts.Seed(account);

        return account;
    }

    private Transfer SeedTransfer(decimal amount)
    {
        Account wallet = SeedAccount("Cash wallet");
        Account savings = SeedAccount("Savings jar");
        Transfer transfer = Transfer.Record(
            Guid.CreateVersion7(),
            wallet.Id,
            savings.Id,
            new Money(amount),
            OccurredOn,
            Now);

        _transfers.Seed(transfer);

        return transfer;
    }

    private Task<Transfer> EditAsync(Guid transferId, EditTransferChanges changes) =>
        new EditTransfer(_transfers, _accounts, _unitOfWork)
            .ExecuteAsync(transferId, changes, CancellationToken.None);
}
