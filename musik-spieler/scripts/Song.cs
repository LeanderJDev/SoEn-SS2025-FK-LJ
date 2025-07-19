using Godot;
using System;

namespace Musikspieler.Scripts
{
    public class Song : ISong
    {
        private readonly string _name;
        public string Name => _name;

        private readonly float _length;
        public float LengthInSeconds => _length;

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
}