using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace YessGoFront.Services;

public class PinStorageService
{
    private const string PinKey = "user_pin";

    public async Task SavePinAsync(string pin)
    {
        await SecureStorage.SetAsync(PinKey, pin);
    }

    public async Task<string?> GetPinAsync()
    {
        return await SecureStorage.GetAsync(PinKey);
    }

    public async Task<bool> ValidatePinAsync(string enteredPin)
    {
        var storedPin = await GetPinAsync();
        return storedPin == enteredPin;
    }

    public async Task ClearPinAsync()
    {
        SecureStorage.Remove(PinKey);
    }
}

