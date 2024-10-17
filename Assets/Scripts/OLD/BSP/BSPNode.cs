using System.Collections.Generic;
using Random = System.Random;

namespace Old
{
    public class BSPNode
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public BSPNode LeftChild { get; private set; }
        public BSPNode RightChild { get; private set; }

        public BSPNode(int x, int y, int width, int height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public void Split(List<BSPNode> leafNodes, Random random, int minRoomSize)
        {
            if (Width <= minRoomSize * 2 || Height <= minRoomSize * 2) {
                leafNodes.Add(this);
                return;
            }

            bool splitHorizontally = random.Next(2) == 0;

            if (splitHorizontally) {
                int splitY = Y + random.Next(minRoomSize, Height - minRoomSize);
                LeftChild = new BSPNode(X, Y, Width, splitY - Y);
                RightChild = new BSPNode(X, splitY, Width, Height - (splitY - Y));
            }
            else {
                int splitX = X + random.Next(minRoomSize, Width - minRoomSize);
                LeftChild = new BSPNode(X, Y, splitX - X, Height);
                RightChild = new BSPNode(splitX, Y, Width - (splitX - X), Height);
            }

            LeftChild.Split(leafNodes, random, minRoomSize);
            RightChild.Split(leafNodes, random, minRoomSize);
        }
    }

}
