using Azure;
using Azure.AI.OpenAI.Assistants;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var configuration = builder.Build();

var endpoint = configuration["ENDPOINT"];
var key = configuration["Key"];

AssistantsClient client = new AssistantsClient(new Uri(endpoint), new AzureKeyCredential(key));

// Create an assistant
Assistant assistant = await client.CreateAssistantAsync(
    new AssistantCreationOptions("gpt4o") // Replace this with the name of your model deployment
    {
        Name = "Math Tutor",
        Instructions = "You are a personal math tutor. Write and run code to answer math questions.",
        Tools = { new CodeInterpreterToolDefinition() } //RetrievalToolDefinition() and FunctionToolDefinition() are also available

    });

// Create a thread
AssistantThread thread = await client.CreateThreadAsync();

// Add a user question to the thread
ThreadMessage message = await client.CreateMessageAsync(
    thread.Id,
    MessageRole.User,
    "I need to solve the equation `3x + 11 = 14`. Can you help me?");

// Run the thread
ThreadRun run = await client.CreateRunAsync(
    thread.Id,
    new CreateRunOptions(assistant.Id)
);

// Wait for the assistant to respond
do
{
    await Task.Delay(TimeSpan.FromMilliseconds(500));
    run = await client.GetRunAsync(thread.Id, run.Id);
}
while (run.Status == RunStatus.Queued
    || run.Status == RunStatus.InProgress);

// Get the messages
PageableList<ThreadMessage> messagesPage = await client.GetMessagesAsync(thread.Id);
IReadOnlyList<ThreadMessage> messages = messagesPage.Data;

// Note: messages iterate from newest to oldest, with the messages[0] being the most recent
foreach (ThreadMessage threadMessage in messages.Reverse())
{
    Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
    foreach (MessageContent contentItem in threadMessage.ContentItems)
    {
        if (contentItem is MessageTextContent textItem)
        {
            Console.Write(textItem.Text);
        }
        Console.WriteLine();
    }
}

