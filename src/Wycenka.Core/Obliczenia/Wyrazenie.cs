using System.Globalization;

namespace Wycenka.Core.Obliczenia;

/// <summary>
/// Prosty kalkulator obmiarów: liczby (przecinek lub kropka), + - * / oraz nawiasy.
/// Np. „4*2,5 + 3” → 13.
/// </summary>
public static class Wyrazenie
{
    public static bool SprobujObliczyc(string? tekst, out decimal wynik)
    {
        wynik = 0;
        if (string.IsNullOrWhiteSpace(tekst)) return false;
        try
        {
            var p = new Parser(tekst);
            wynik = p.Calosc();
            return true;
        }
        catch (Exception e) when (e is FormatException or DivideByZeroException or OverflowException)
        {
            return false;
        }
    }

    private sealed class Parser(string tekst)
    {
        private readonly string _s = tekst.Replace(" ", "").Replace(" ", "").Replace(" ", "");
        private int _i;

        public decimal Calosc()
        {
            var v = Suma();
            if (_i != _s.Length) throw new FormatException();
            return v;
        }

        private decimal Suma()
        {
            var v = Iloczyn();
            while (_i < _s.Length && _s[_i] is '+' or '-')
            {
                var op = _s[_i++];
                var r = Iloczyn();
                v = op == '+' ? v + r : v - r;
            }
            return v;
        }

        private decimal Iloczyn()
        {
            var v = Czynnik();
            while (_i < _s.Length && _s[_i] is '*' or '/' or 'x' or '×' or ':')
            {
                var op = _s[_i++];
                var r = Czynnik();
                v = op is '/' or ':' ? v / r : v * r;
            }
            return v;
        }

        private decimal Czynnik()
        {
            if (_i >= _s.Length) throw new FormatException();
            if (_s[_i] == '-') { _i++; return -Czynnik(); }
            if (_s[_i] == '+') { _i++; return Czynnik(); }
            if (_s[_i] == '(')
            {
                _i++;
                var v = Suma();
                if (_i >= _s.Length || _s[_i] != ')') throw new FormatException();
                _i++;
                return v;
            }

            var start = _i;
            while (_i < _s.Length && (char.IsDigit(_s[_i]) || _s[_i] is ',' or '.')) _i++;
            if (start == _i) throw new FormatException();
            var liczba = _s[start.._i].Replace(',', '.');
            return decimal.Parse(liczba, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }
    }
}
