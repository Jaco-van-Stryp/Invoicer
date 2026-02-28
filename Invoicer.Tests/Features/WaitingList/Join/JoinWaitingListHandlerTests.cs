using FluentAssertions;
using Invoicer.Features.WaitingList.Join;
using Invoicer.Infrastructure.EmailService;
using Invoicer.Infrastructure.EmailTemplateService;
using Invoicer.Infrastructure.EmailValidationService;
using Invoicer.Tests.Infrastructure;
using NSubstitute;

namespace Invoicer.Tests.Features.WaitingList.Join;

[Collection("Database")]
public class JoinWaitingListHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private JoinWaitingListHandler CreateHandler(
        IEmailValidationService? emailValidationService = null,
        IEmailService? emailService = null,
        IEmailTemplateService? emailTemplateService = null
    )
    {
        var validation = emailValidationService ?? Substitute.For<IEmailValidationService>();
        var email = emailService ?? Substitute.For<IEmailService>();
        var template = emailTemplateService ?? Substitute.For<IEmailTemplateService>();
        return new JoinWaitingListHandler(DbContext, validation, email, template);
    }

    [Fact]
    public async Task Handle_ValidEmail_AddsToWaitingListAndSendsEmail()
    {
        // Arrange
        var emailValidation = Substitute.For<IEmailValidationService>();
        emailValidation.IsValidEmail("user@example.com").Returns(true);

        var emailService = Substitute.For<IEmailService>();
        emailService.SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var emailTemplate = Substitute.For<IEmailTemplateService>();
        emailTemplate
            .RenderTemplate(Arg.Any<EmailTemplateName>(), Arg.Any<Dictionary<string, string>>())
            .Returns("<html>welcome</html>");

        var handler = CreateHandler(emailValidation, emailService, emailTemplate);

        // Act
        await handler.Handle(new JoinWaitingListCommand("user@example.com"), CancellationToken.None);

        // Assert — added to DB
        DbContext.ChangeTracker.Clear();
        var entry = await DbContext.WaitingList.FindAsync(
            DbContext.WaitingList.FirstOrDefault(w => w.Email == "user@example.com")?.Id
        );
        var saved = DbContext.WaitingList.FirstOrDefault(w => w.Email == "user@example.com");
        saved.Should().NotBeNull("email should be in the waiting list");

        // Assert — confirmation email sent
        await emailService
            .Received(1)
            .SendEmailAsync(
                Arg.Is<string>(to => to == "user@example.com"),
                Arg.Any<string>(),
                Arg.Any<string>()
            );
    }

    [Fact]
    public async Task Handle_DuplicateEmail_DoesNotAddSecondEntry()
    {
        // Arrange
        var emailValidation = Substitute.For<IEmailValidationService>();
        emailValidation.IsValidEmail("user@example.com").Returns(true);

        var emailService = Substitute.For<IEmailService>();
        emailService.SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var emailTemplate = Substitute.For<IEmailTemplateService>();
        emailTemplate
            .RenderTemplate(Arg.Any<EmailTemplateName>(), Arg.Any<Dictionary<string, string>>())
            .Returns("<html>welcome</html>");

        var handler = CreateHandler(emailValidation, emailService, emailTemplate);

        // Act — join twice
        await handler.Handle(new JoinWaitingListCommand("user@example.com"), CancellationToken.None);
        await handler.Handle(new JoinWaitingListCommand("user@example.com"), CancellationToken.None);

        // Assert — only one entry in DB
        DbContext.ChangeTracker.Clear();
        var count = DbContext.WaitingList.Count(w => w.Email == "user@example.com");
        count.Should().Be(1, "duplicate emails should not be added");

        // Assert — email sent only once (first join)
        await emailService
            .Received(1)
            .SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_InvalidEmail_DoesNotAddToWaitingList()
    {
        // Arrange
        var emailValidation = Substitute.For<IEmailValidationService>();
        emailValidation.IsValidEmail("not-a-real-email").Returns(false);

        var emailService = Substitute.For<IEmailService>();
        var handler = CreateHandler(emailValidation, emailService);

        // Act
        await handler.Handle(new JoinWaitingListCommand("not-a-real-email"), CancellationToken.None);

        // Assert — nothing added to DB
        DbContext.ChangeTracker.Clear();
        var saved = DbContext.WaitingList.FirstOrDefault(w => w.Email == "not-a-real-email");
        saved.Should().BeNull("invalid emails should not be added");

        // Assert — no email sent
        await emailService
            .DidNotReceive()
            .SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_TwoDifferentValidEmails_AddsBothToWaitingList()
    {
        // Arrange
        var emailValidation = Substitute.For<IEmailValidationService>();
        emailValidation.IsValidEmail(Arg.Any<string>()).Returns(true);

        var emailService = Substitute.For<IEmailService>();
        emailService.SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var emailTemplate = Substitute.For<IEmailTemplateService>();
        emailTemplate
            .RenderTemplate(Arg.Any<EmailTemplateName>(), Arg.Any<Dictionary<string, string>>())
            .Returns("<html>welcome</html>");

        var handler = CreateHandler(emailValidation, emailService, emailTemplate);

        // Act
        await handler.Handle(new JoinWaitingListCommand("alice@example.com"), CancellationToken.None);
        await handler.Handle(new JoinWaitingListCommand("bob@example.com"), CancellationToken.None);

        // Assert — both entries in DB
        DbContext.ChangeTracker.Clear();
        var count = DbContext.WaitingList.Count(w =>
            w.Email == "alice@example.com" || w.Email == "bob@example.com"
        );
        count.Should().Be(2);

        // Assert — two emails sent
        await emailService
            .Received(2)
            .SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
