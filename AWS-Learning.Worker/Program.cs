using AWS_Learning;
using AWS_Learning.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLearningServices();
builder.Services.AddLearningAws(builder.Configuration, useWorkerProfile: true);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
