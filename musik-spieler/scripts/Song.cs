using Godot;
using System;

namespace Musikspieler.Scripts
{
    public class Song : ISong
    {
        private readonly string _name;
        public string Name => _name;

        public override string ToString()
        {
            return $"{nameof(Song)}: {_name}";
        }

        public Song(string name)
        {
            _name = name;
        }

        //...
    }

    public interface ISong
    {
        public string Name { get; }
    }
}