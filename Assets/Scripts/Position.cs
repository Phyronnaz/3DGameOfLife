using UnityEngine;

namespace Assets.Scripts
{
    public struct Position
    {
        public int x;
        public int y;
        public int z;
        public Position(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Position(Vector3 vector)
        {
            return new Position((int)vector.x, (int)vector.y, (int)vector.z);
        }

        public static implicit operator Vector3(Position position)
        {
            return new Vector3(position.x, position.y, position.z);
        }

        public static Position operator *(Position position, int i)
        {
            return new Position(position.x * i, position.y * i, position.z * i);
        }

        public static Position operator *(int i, Position position)
        {
            return position * i;
        }

        public static Position operator /(Position position, int i)
        {
            return new Position(position.x / i, position.y / i, position.z / i);
        }
        public static Position operator %(Position position, int i)
        {
            return new Position(position.x % i, position.y % i, position.z % i);
        }
    }
}
