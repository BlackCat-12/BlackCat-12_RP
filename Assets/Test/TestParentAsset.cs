using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Scripts
{
    [CreateAssetMenu(menuName = "TestParentAsset", fileName = "Test")]
    public class TestParentAsset : ScriptableObject
    {
        public List<TestChildAsset> testChild;

        public void AddChild(TestChildAsset child)
        {
            if (testChild == null)
            {
                testChild = new List<TestChildAsset>();
            }
            testChild.Add(child);
        }
    }
}