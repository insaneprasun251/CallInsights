using Core.Constants;
using Core.Helpers;
using Core.Options;
using Core.Services;
using DataLayer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceLayer;
using ServiceLayer.Pipeline;
using System.Text.Json.Serialization;
using UI.Web.Api.ExceptionHandling;
using UI.Web.Api.Hubs;
using UI.Web.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

// Upload limit: one source, the "Storage:MaxFileSizeMb" setting.
// Kestrel's limit gets 1 MB of headroom for the multipart framing; the exact check is done in RecordingService.
var storage = builder.Configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
var maxUploadBytes = FileSizeHelper.MbToBytes(storage.MaxFileSizeMb);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = maxUploadBytes + FileSizeHelper.MbToBytes(1));
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = maxUploadBytes + FileSizeHelper.MbToBytes(1));

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mysql => mysql.MigrationsHistoryTable(DbTables.MigrationsHistory)));

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddScoped<IRecordingService, RecordingService>();


builder.Services.Configure<ProcessingOptions>(builder.Configuration.GetSection(ProcessingOptions.SectionName));
builder.Services.AddSingleton<IProcessingQueue, ProcessingQueue>();
builder.Services.AddScoped<ICallPipeline, CallPipeline>();
builder.Services.AddScoped<IPipelineStep, PreprocessStep>();
builder.Services.AddScoped<IPipelineStep>(services => services.GetRequiredService<TranscribeStep>());
builder.Services.AddScoped<IPipelineStep>(services => services.GetRequiredService<SpeakerLabelStep>());
builder.Services.AddScoped<IPipelineStep, PromptBuildStep>();
builder.Services.AddScoped<IPipelineStep>(services => services.GetRequiredService<InsightsStep>());
builder.Services.AddScoped<ITranscriptionService, TranscriptionService>();
builder.Services.AddScoped<IInsightsService, InsightsService>();
builder.Services.AddHostedService<ProcessingWorker>();

builder.Services.Configure<TranscriptionOptions>(builder.Configuration.GetSection(TranscriptionOptions.SectionName));
builder.Services.Configure<DiarizationOptions>(builder.Configuration.GetSection(DiarizationOptions.SectionName));
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.SectionName));

// the transcription and the speaker steps talk to the same hosted GPU server
static void ConfigureHostedServerClient(IServiceProvider services, HttpClient client)
{
    var options = services.GetRequiredService<IOptions<TranscriptionOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 30, 660));
}

builder.Services.AddHttpClient<TranscribeStep>(ConfigureHostedServerClient);
builder.Services.AddHttpClient<SpeakerLabelStep>(ConfigureHostedServerClient);
builder.Services.AddHttpClient<InsightsStep>(ConfigureHostedServerClient);

builder.Services.Configure<AudioOptions>(builder.Configuration.GetSection(AudioOptions.SectionName));

builder.Services.AddSignalR();
builder.Services.AddScoped<IRecordingNotifier, SignalRRecordingNotifier>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<RecordingsHub>(HubDefinitions.RecordingsPath);

app.Run();
