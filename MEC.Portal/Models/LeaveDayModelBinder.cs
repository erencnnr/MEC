using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MEC.Portal.Models;

// HTML number inputs send a dot even on Turkish browsers. Never interpret it as a thousands separator.
public sealed class LeaveDayModelBinder : IModelBinder
{
    public static bool TryParse(string? text, out decimal value) => decimal.TryParse(text?.Trim().Replace(',', '.'),
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    public Task BindModelAsync(ModelBindingContext context)
    {
        var input=context.ValueProvider.GetValue(context.ModelName);
        context.ModelState.SetModelValue(context.ModelName,input);
        if(TryParse(input.FirstValue,out var value)) context.Result=ModelBindingResult.Success(value);
        else context.ModelState.TryAddModelError(context.ModelName,"Geçerli gün sayısı girin.");
        return Task.CompletedTask;
    }
}
