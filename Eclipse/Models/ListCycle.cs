using System;

using System.Collections.Generic;

namespace Eclipse.Models
{
    public class ListCycle<T>
    {
        // list of games or whatever you want
        public List<T> GenericList { get; set; }

        // indexes currently available to display
        // will cycle around the size of the generic list
        public int[] indices;

        public ListCycle(List<T> genericList, int activeCycleCount)
        {
            GenericList = genericList;
            indices = new int[activeCycleCount];
            InitializeIndices();
        }

        private void InitializeIndices()
        {
            if (GenericList != null)
            {
                int lastIndex = -1;
                for (int i = 0; i < indices.Length; i++)
                {
                    indices[i] = GetNextIndex(lastIndex);
                    lastIndex = indices[i];
                }
            }
        }


        public void SetCurrentIndex(int index, bool oneBased = false)
        {
            if(oneBased)
            {
                indices[0] = GetPreviousIndex(index);
            }
            else
            {
                indices[0] = index;
            }

            int lastIndex = indices[0];
            for(int i = 1; i < indices.Length; i++)
            {
                indices[i] = GetNextIndex(lastIndex);
                lastIndex = indices[i];
            }
        }


        private int GetNextIndex(int currentIndex)
        {
            currentIndex += 1;
            if (currentIndex == GenericList.Count)
            {
                currentIndex = 0;
            }
            return currentIndex;
        }

        private int GetPreviousIndex(int currentIndex)
        {
            currentIndex -= 1;
            if (currentIndex == -1)
            {
                currentIndex = GenericList.Count - 1;
            }
            return currentIndex;
        }

        public int GetIndexValue(int index)
        {
            return indices[index];
        }

        public T GetItem(int index)
        {
            return GenericList[indices[index]];
        }

        public void CycleForward()
        {
            for (int i = 0; i < indices.Length; i++)
            {
                if (i + 1 < indices.Length - 1)
                {
                    indices[i] = indices[i + 1];
                }
                else
                {
                    indices[i] = GetNextIndex(indices[i]);
                }
            }
        }

        public void CycleBackward()
        {
            for (int i = indices.Length - 1; i >= 0; i--)
            {
                if(i - 1 >= 0)
                {
                    indices[i] = indices[i - 1];
                }
                else
                {
                    indices[i] = GetPreviousIndex(indices[i]);
                }
            }
        }
    }
}
