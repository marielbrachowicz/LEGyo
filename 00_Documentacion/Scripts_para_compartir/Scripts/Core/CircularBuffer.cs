using UnityEngine;

namespace ARMyo.Core
{
    /// <summary>
    /// Buffer circular de Vector3 con promedio móvil, sin generar basura en runtime.
    /// Copiado del proyecto del brazo (MagicMirror.Core.CircularBuffer).
    /// Add() devuelve el promedio actualizado en O(1).
    /// </summary>
    public sealed class CircularBuffer
    {
        private readonly Vector3[] _data;
        private readonly int _capacity;
        private int _head;
        private int _count;
        private Vector3 _sum;

        public int Count => _count;
        public bool IsFull => _count == _capacity;

        public CircularBuffer(int capacity)
        {
            _capacity = Mathf.Max(1, capacity);
            _data = new Vector3[_capacity];
        }

        public Vector3 Add(Vector3 value)
        {
            if (_count == _capacity) _sum -= _data[_head];
            else _count++;

            _data[_head] = value;
            _sum += value;
            _head = (_head + 1) % _capacity;
            return _sum / _count;
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
            _sum = Vector3.zero;
        }
    }
}
