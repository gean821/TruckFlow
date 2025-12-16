using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TruckFlow.Application.Validators.EanValidators
{
    public static class EanValidator
    {
        public static bool IsValid(string ean)
        {
            if (string.IsNullOrWhiteSpace(ean)) return false;

            // Só números
            if (!Regex.IsMatch(ean, @"^\d{8}$|^\d{13}$"))
                return false;

            return ValidateChecksum(ean);
        }

        private static bool ValidateChecksum(string ean)
        {
            int sum = 0;
            bool triple = true;

            for (int i = ean.Length - 2; i >= 0; i--)
            {
                int digit = ean[i] - '0';
                sum += digit * (triple ? 3 : 1);
                triple = !triple;
            }

            int checkDigit = (10 - (sum % 10)) % 10;

            return checkDigit == (ean[^1] - '0');
        }
    }
}

