using System.CommandLine;
using GraderCli.UseCases;

namespace GraderCli.Commands;

/// <summary>
/// Creates the commands of the use cases: <c>grader {area} {verb} {ids...}</c>, <c>--file</c> for use cases with a
/// document, <c>--yes</c> for destructive use cases.
/// </summary>
internal static class UseCaseCommands
{
    public static Command CreateAreaCommand(UseCaseArea area)
    {
        var command = new GraderCommand(area.Name, area.Description);
        foreach (var useCase in area.UseCases)
        {
            command.Subcommands.Add(CreateCommand(area, useCase));
        }

        return command;
    }

    private static GraderCommand CreateCommand(UseCaseArea area, UseCase useCase)
    {
        var command = new GraderCommand(useCase.Verb, useCase.Description, useCase.Examples);
        var ids = useCase.Ids.Select(id => new Argument<int>(id.Name) { Description = id.Description }).ToList();
        ids.ForEach(command.Arguments.Add);

        Option<string>? file = null;
        if (useCase.DocumentSchema is not null)
        {
            file = GraderCommand.CreateFileOption();
            command.Options.Add(file);
        }

        Option<bool>? yes = null;
        if (useCase.Effect == UseCaseEffect.Delete)
        {
            yes = GraderCommand.CreateYesOption();
            command.Options.Add(yes);
        }

        command.SetApiAction(async (call, ct) =>
        {
            var idValues = ids.Select(call.GetValue).ToList();
            if (yes is not null)
            {
                call.RequireConfirmation(yes, $"grader {area.Name} {useCase.Verb}");
            }

            var document = file is null ? null : await call.ReadDocumentAsync(call.GetValue(file), ct);
            var json = await call.CallAsync(useCase, idValues, document, ct);
            await call.WriteAsync(useCase.Response, json, idValues, ct);
        });
        return command;
    }
}
