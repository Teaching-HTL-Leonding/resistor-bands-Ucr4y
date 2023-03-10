using System.Collections.Concurrent;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });
});
var app = builder.Build();

var colors = new ConcurrentDictionary<string, ColorDetails>()
{
    ["black"] = new ColorDetails(0, 1, 0),
    ["brown"] = new ColorDetails(1, 10, 1),
    ["red"] = new ColorDetails(2, 100, 2),
    ["orange"] = new ColorDetails(3, 1000, 0),
    ["yellow"] = new ColorDetails(4, 10000, 0),
    ["green"] = new ColorDetails(5, 100000, 0.5),
    ["blue"] = new ColorDetails(6, 1000000, 0.25),
    ["violet"] = new ColorDetails(7, 10000000, 0.10),
    ["grey"] = new ColorDetails(8, 100000000, 0.05),
    ["white"] = new ColorDetails(8, 100000000, 0.05),
    ["gold"] = new ColorDetails(0, 0.1, 5),
    ["silver"] = new ColorDetails(0, 0.01, 10)
};
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/colors", () => colors.Keys).WithTags("Colors").WithOpenApi(o =>
{
    o.Summary = """Return all colors for bands on resistors""";
    return o;
})
.Produces(StatusCodes.Status200OK)
    .WithOpenApi(o =>
    {
        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "A list of all colors";
        return o;
    });

app.MapGet("/colors/{color}", (string color) =>
{
    if (!colors.TryGetValue(color, out ColorDetails? colorDetails))
    {
        return Results.NotFound();
    }
    return Results.Ok(colorDetails);
})
.WithTags("Colors")
.WithOpenApi(o =>
{
    o.Summary = """Return details for a color band""";
    return o;
})
    .Produces<ColorDetails>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi(o =>
    {
        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "Details for a color band";
        o.Responses[((int)StatusCodes.Status404NotFound).ToString()].Description = "Unknown Color";
        return o;
    });

app.MapPost("/resistors/value-from-bands", (ResistorBands resistorBands) =>
{
    return GetResistorValues(resistorBands);
})
.WithTags("Resistors")
.WithOpenApi(o =>
{
    o.Summary = """Calculates the resistor value based on given color bands (using POST).""";
    return o;
})
    .Produces<ResistorValue>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi(o =>
    {
        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "Resistor value could be decoded correctly";
        o.Responses[((int)StatusCodes.Status404NotFound).ToString()].Description = "The request body contains invalid data";
        return o;
    });

app.MapGet("/resistors/value-from-bands", (string firstBand, string secondBand, string? thirdBand, string multiplier, string tolerance) =>
{
    return GetResistorValues(new ResistorBands(firstBand, secondBand, thirdBand, multiplier, tolerance));
})
.WithTags("Resistors")
.WithOpenApi(o =>
{
    o.Summary = """Calculates the resistor value based on given color bands (using GET).""";
    return o;
})
    .Produces<ResistorValue>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi(o =>
    {
        o.Parameters[0].Description = "Color of the 1st band";
        o.Parameters[1].Description = "Color of the 2nd band";
        o.Parameters[2].Description = "Color of the 3rd band. Note that this band can be left out for 4-band-coded resistors.";
        o.Parameters[3].Description = "Color of the multiplier band";
        o.Parameters[4].Description = "Color of the tolerance band";

        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "Resistor value could be decoded correctly";
        o.Responses[((int)StatusCodes.Status404NotFound).ToString()].Description = "The request body contains invalid data";
        return o;
    });

app.Run();

IResult GetResistorValues(ResistorBands resistorBands)
{
    double value = 0;

    colors.TryGetValue(resistorBands.firstBand, out ColorDetails? firstBandDetails);
    colors.TryGetValue(resistorBands.secondBand, out ColorDetails? secondBandDetails);
    if (resistorBands.thirdBand != null)
    {
        colors.TryGetValue(resistorBands.thirdBand, out ColorDetails? thirdBandDetails);
        value += firstBandDetails.value * 100 + secondBandDetails.value * 10 + thirdBandDetails.value;
    }
    else
    {
        value += firstBandDetails.value * 10 + secondBandDetails.value;
    }
    colors.TryGetValue(resistorBands.multiplier, out ColorDetails multiplier);
    value *= multiplier.multiplier;
    colors.TryGetValue(resistorBands.tolerance, out ColorDetails tolerance);
    return Results.Ok(new ResistorValue(value, tolerance.tolerance));

}

/// <summary>
/// Test
/// </summary>
record ColorDetails(int value, double multiplier, double tolerance);

record ResistorBands(string firstBand, string secondBand, string thirdBand, string multiplier, string tolerance);

record ResistorValue(double resistorValue, double tolerance);