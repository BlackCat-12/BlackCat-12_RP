using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class TestChildAsset : ScriptableObject
    {
        public List<string> testChrid;

        public TestChildAsset()
        {
            testChrid = new List<string>();
        }
    }
}