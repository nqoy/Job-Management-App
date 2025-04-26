using MainServer.StartupInitialization;

var builder = WebApplication.CreateBuilder(args);

AppInitializer.ConfigureLogging(builder);
AppInitializer.ConfigureServices(builder);

var app = builder.Build();

AppInitializer.ConfigureApp(app);

AppInitializer.LogServerUrls(app);

app.Run();
