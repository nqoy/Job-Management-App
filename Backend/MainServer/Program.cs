using MainServer.Initialization;

var builder = WebApplication.CreateBuilder(args);

AppInitializer.ConfigureServices(builder);
var app = builder.Build();

AppInitializer.ConfigureApp(app);
AppInitializer.LogServerUrls(app);

app.Run();