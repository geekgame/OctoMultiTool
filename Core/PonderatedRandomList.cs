namespace ConsoleApp1.Core
{

    public class PonderatedRandomList<T> where T : notnull
    {
        public Dictionary<T, int> _list = new Dictionary<T, int>();
        public int _total = 0;

        Action<T>? HookGet = null;

        public void Add(T e, int multiplier = 1)
        {
            if (!_list.ContainsKey(e)) _list[e] = 0;
            _list[e] += multiplier;

            _total += multiplier;
        }

        public T? get()
        {
            // Get a random element based on the ponderation of each element in the list    
            if (_total == 0) return default(T);

            int r = new Random().Next(0, _total);
            int c = 0;

            foreach (KeyValuePair<T, int> e in _list)
            {
                c += e.Value;
                if (c > r)
                {
                    HookGet?.Invoke(e.Key);
                    return e.Key;
                }
            }

            return default(T);
        }

        public void remove(T e, int multiplier = 1)
        {
            if (!_list.ContainsKey(e)) return;
            _list[e] -= multiplier;

            if (_list[e] <= 0)
                _list.Remove(e);

            _total -= multiplier;
        }

        public void show_probabilities()
        {
            // Show the probability of each element in the list to be selected as a graph

            if (_total == 0) return;

            var colors = new List<string>() { "Red", "Green", "Blue", "Yellow", "Cyan", "Magenta", "White", "Gray" };
            foreach (KeyValuePair<T, int> e in _list)
            {
                Console.ResetColor();
                Console.Write(e.Key + ": ");

                int p = (int)((float)e.Value / (float)_total * 100);

                Console.ForegroundColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), colors[_list.Keys.ToList().IndexOf(e.Key)]);
                for (int i = 0; i < p; i++)
                {
                    Console.Write("=");
                }

                Console.WriteLine(" " + p + "%");
            }

            Console.ResetColor();
            Console.WriteLine("Total: " + _total);

        }

    }

}
