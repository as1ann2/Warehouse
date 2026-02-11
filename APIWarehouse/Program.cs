using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();


app.UseCors();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// In-memory база данных
var items = new List<Item>();
int nextId = 1;


// crud
app.MapGet("/items", () => items)
    .WithName("GetAllItems")
    .WithTags("Items");

app.MapGet("/items/{id:int}", (int id) =>
{
    var item = items.FirstOrDefault(i => i.Id == id);
    return item != null ? Results.Ok(item) : Results.NotFound();
})
.WithName("GetItemById")
.WithTags("Items");

app.MapPost("/items", (Item item) =>
{
    item.Id = nextId++;
    items.Add(item);
    return Results.Created($"/items/{item.Id}", item);
})
.WithName("CreateItem")
.WithTags("Items");

app.MapPut("/items/{id:int}", (int id, Item updatedItem) =>
{
    var item = items.FirstOrDefault(i => i.Id == id);
    if (item == null) return Results.NotFound();

    item.Name = updatedItem.Name;
    item.Quantity = updatedItem.Quantity;
    return Results.Ok(item);
})
.WithName("UpdateItem")
.WithTags("Items");

app.MapDelete("/items/{id:int}", (int id) =>
{
    var item = items.FirstOrDefault(i => i.Id == id);
    if (item == null) return Results.NotFound();

    items.Remove(item);
    return Results.Ok();
})
.WithName("DeleteItem")
.WithTags("Items");

app.Run();


public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
