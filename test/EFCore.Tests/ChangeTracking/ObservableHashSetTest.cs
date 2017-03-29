// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Internal;
using Xunit;

namespace Microsoft.EntityFrameworkCore.Tests.ChangeTracking
{
    public class ObservableHashSetTest
    {
        private static readonly Random _random = new Random();

        [Fact]
        public void Can_construct()
        {
            Assert.Same(
                new HashSet<int>().Comparer,
                new ObservableHashSet<int>().Comparer);

            Assert.Same(
                ReferenceEqualityComparer.Instance,
                new ObservableHashSet<object>(ReferenceEqualityComparer.Instance).Comparer);

            var testData1 = CreateTestData();

            var rh1 = new HashSet<int>(testData1);
            var ohs1 = new ObservableHashSet<int>(testData1);
            Assert.Equal(rh1.OrderBy(i => i), ohs1.OrderBy(i => i));
            Assert.Same(rh1.Comparer, ohs1.Comparer);

            var testData2 = CreateTestData().Cast<object>();

            var rh2 = new HashSet<object>(testData2, ReferenceEqualityComparer.Instance);
            var ohs2 = new ObservableHashSet<object>(testData2, ReferenceEqualityComparer.Instance);
            Assert.Equal(rh2.OrderBy(i => i), ohs2.OrderBy(i => i));
            Assert.Same(rh2.Comparer, ohs2.Comparer);
        }

        [Fact]
        public void Can_add()
        {
            var hashSet = new ObservableHashSet<string>();
            var countChanging = 0;
            var countChanged = 0;
            var collectionChanged = 0;
            var currentCount = 0;
            var countChange = 1;
            var adding = new string[0];

            hashSet.PropertyChanging += (s, a) => AssertCountChanging(hashSet, s, a, currentCount, ref countChanging);
            hashSet.PropertyChanged += (s, a) => AssertCountChanged(hashSet, s, a, ref currentCount, countChange, ref countChanged);
            hashSet.CollectionChanged += (s, a) =>
                {
                    Assert.Equal(NotifyCollectionChangedAction.Add, a.Action);
                    Assert.Null(a.OldItems);
                    Assert.Equal(adding, a.NewItems.OfType<string>());
                    collectionChanged++;
                };

            adding = new[] { "Palmer" };
            Assert.True(hashSet.Add("Palmer"));

            Assert.Equal(1, countChanging);
            Assert.Equal(1, countChanged);
            Assert.Equal(1, collectionChanged);
            Assert.Equal(new[] { "Palmer" }, hashSet);

            adding = new[] { "Carmack" };
            Assert.True(hashSet.Add("Carmack"));

            Assert.Equal(2, countChanging);
            Assert.Equal(2, countChanged);
            Assert.Equal(2, collectionChanged);
            Assert.Equal(new[] { "Carmack", "Palmer" }, hashSet.OrderBy(i => i));

            Assert.False(hashSet.Add("Palmer"));

            Assert.Equal(2, countChanging);
            Assert.Equal(2, countChanged);
            Assert.Equal(2, collectionChanged);
            Assert.Equal(new[] { "Carmack", "Palmer" }, hashSet.OrderBy(i => i));
        }

        [Fact]
        public void Can_clear()
        {
            var testData = new HashSet<int>(CreateTestData());

            var hashSet = new ObservableHashSet<int>(testData);
            var countChanging = 0;
            var countChanged = 0;
            var collectionChanged = 0;
            var currentCount = testData.Count;
            var countChange = -testData.Count;

            hashSet.PropertyChanging += (s, a) => AssertCountChanging(hashSet, s, a, currentCount, ref countChanging);
            hashSet.PropertyChanged += (s, a) => AssertCountChanged(hashSet, s, a, ref currentCount, countChange, ref countChanged);
            hashSet.CollectionChanged += (s, a) =>
                {
                    Assert.Equal(NotifyCollectionChangedAction.Replace, a.Action);
                    Assert.Equal(testData.OrderBy(i => i), a.OldItems.OfType<int>().OrderBy(i => i));
                    Assert.Empty(a.NewItems);
                    collectionChanged++;
                };

            hashSet.Clear();

            Assert.Equal(testData.Count == 0 ? 0 : 1, countChanging);
            Assert.Equal(testData.Count == 0 ? 0 : 1, countChanged);
            Assert.Equal(testData.Count == 0 ? 0 : 1, collectionChanged);
            Assert.Empty(hashSet);

            hashSet.Clear();

            Assert.Equal(testData.Count == 0 ? 0 : 1, countChanging);
            Assert.Equal(testData.Count == 0 ? 0 : 1, countChanged);
            Assert.Equal(testData.Count == 0 ? 0 : 1, collectionChanged);
            Assert.Empty(hashSet);
        }

        [Fact]
        public void Contains_works()
        {
            var testData = CreateTestData();
            var hashSet = new ObservableHashSet<int>(testData);

            foreach (var item in testData)
            {
                Assert.True(hashSet.Contains(item));
            }

            foreach (var item in CreateTestData(1000, 10000).Except(testData))
            {
                Assert.False(hashSet.Contains(item));
            }
        }

        [Fact]
        public void Can_copy_to_array()
        {
            var testData = CreateTestData();
            var orderedDistinct = testData.Distinct().OrderBy(i => i).ToList();

            var hashSet = new ObservableHashSet<int>(testData);

            Assert.Equal(orderedDistinct.Count, hashSet.Count);

            var array = new int[hashSet.Count];
            hashSet.CopyTo(array);

            Assert.Equal(orderedDistinct, array.OrderBy(i => i));

            array = new int[hashSet.Count + 100];
            hashSet.CopyTo(array, 100);

            Assert.Equal(orderedDistinct, array.Skip(100).OrderBy(i => i));

            var toTake = Math.Min(10, hashSet.Count);
            array = new int[100 + toTake];
            hashSet.CopyTo(array, 100, toTake);

            foreach (var value in array.Skip(100).Take(toTake))
            {
                Assert.Contains(value, hashSet);
            }
        }

        [Fact]
        public void Can_remove()
        {
            var hashSet = new ObservableHashSet<string> { "Palmer", "Carmack" };
            var countChanging = 0;
            var countChanged = 0;
            var collectionChanged = 0;
            var currentCount = 2;
            var countChange = -1;
            var removing = new string[0];

            hashSet.PropertyChanging += (s, a) => AssertCountChanging(hashSet, s, a, currentCount, ref countChanging);
            hashSet.PropertyChanged += (s, a) => AssertCountChanged(hashSet, s, a, ref currentCount, countChange, ref countChanged);
            hashSet.CollectionChanged += (s, a) =>
                {
                    Assert.Equal(NotifyCollectionChangedAction.Remove, a.Action);
                    Assert.Equal(removing, a.OldItems.OfType<string>());
                    Assert.Null(a.NewItems);
                    collectionChanged++;
                };

            removing = new[] { "Palmer" };
            Assert.True(hashSet.Remove("Palmer"));

            Assert.Equal(1, countChanging);
            Assert.Equal(1, countChanged);
            Assert.Equal(1, collectionChanged);
            Assert.Equal(new[] { "Carmack" }, hashSet);

            removing = new[] { "Carmack" };
            Assert.True(hashSet.Remove("Carmack"));

            Assert.Equal(2, countChanging);
            Assert.Equal(2, countChanged);
            Assert.Equal(2, collectionChanged);
            Assert.Empty(hashSet);

            Assert.False(hashSet.Remove("Palmer"));

            Assert.Equal(2, countChanging);
            Assert.Equal(2, countChanged);
            Assert.Equal(2, collectionChanged);
            Assert.Empty(hashSet);
        }

        [Fact]
        public void Not_read_only()
        {
            Assert.False(new ObservableHashSet<Random>().IsReadOnly);
        }

        [Fact]
        public void Can_union_with()
        {
            var hashSet = new ObservableHashSet<string> { "Palmer", "Carmack" };
            var countChanging = 0;
            var countChanged = 0;
            var collectionChanged = 0;
            var currentCount = 2;
            var countChange = 2;
            var adding = new[] { "Brendan", "Nate" };

            hashSet.PropertyChanging += (s, a) => AssertCountChanging(hashSet, s, a, currentCount, ref countChanging);
            hashSet.PropertyChanged += (s, a) => AssertCountChanged(hashSet, s, a, ref currentCount, countChange, ref countChanged);
            hashSet.CollectionChanged += (s, a) =>
                {
                    Assert.Equal(NotifyCollectionChangedAction.Replace, a.Action);
                    Assert.Empty(a.OldItems);
                    Assert.Equal(adding, a.NewItems.OfType<string>().OrderBy(i => i));
                    collectionChanged++;
                };

            hashSet.UnionWith(new[] { "Carmack", "Nate", "Brendan" });

            Assert.Equal(1, countChanging);
            Assert.Equal(1, countChanged);
            Assert.Equal(1, collectionChanged);
            Assert.Equal(new[] { "Brendan", "Carmack", "Nate", "Palmer" }, hashSet.OrderBy(i => i));

            hashSet.UnionWith(new[] { "Brendan" });

            Assert.Equal(1, countChanging);
            Assert.Equal(1, countChanged);
            Assert.Equal(1, collectionChanged);
            Assert.Equal(new[] { "Brendan", "Carmack", "Nate", "Palmer" }, hashSet.OrderBy(i => i));
        }

        [Fact]
        public void Can_intersect_with()
        {
            var hashSet = new ObservableHashSet<string> { "Brendan", "Carmack", "Nate", "Palmer" };
            var countChanging = 0;
            var countChanged = 0;
            var collectionChanged = 0;
            var currentCount = 4;
            var countChange = -2;
            var removing = new[] { "Brendan", "Nate" };

            hashSet.PropertyChanging += (s, a) => AssertCountChanging(hashSet, s, a, currentCount, ref countChanging);
            hashSet.PropertyChanged += (s, a) => AssertCountChanged(hashSet, s, a, ref currentCount, countChange, ref countChanged);
            hashSet.CollectionChanged += (s, a) =>
                {
                    Assert.Equal(NotifyCollectionChangedAction.Replace, a.Action);
                    Assert.Equal(removing, a.OldItems.OfType<string>().OrderBy(i => i));
                    Assert.Empty(a.NewItems);
                    collectionChanged++;
                };

            hashSet.IntersectWith(new[] { "Carmack", "Palmer", "Abrash" });

            Assert.Equal(1, countChanging);
            Assert.Equal(1, countChanged);
            Assert.Equal(1, collectionChanged);
            Assert.Equal(new[] { "Carmack", "Palmer" }, hashSet.OrderBy(i => i));

            hashSet.IntersectWith(new[] { "Carmack", "Palmer", "Abrash" });

            Assert.Equal(1, countChanging);
            Assert.Equal(1, countChanged);
            Assert.Equal(1, collectionChanged);
            Assert.Equal(new[] { "Carmack", "Palmer" }, hashSet.OrderBy(i => i));
        }

        [Fact]
        public void Can_except_with()
        {
            var hashSet = new ObservableHashSet<string> { "Brendan", "Carmack", "Nate", "Palmer" };
            var countChanging = 0;
            var countChanged = 0;
            var collectionChanged = 0;
            var currentCount = 4;
            var countChange = -2;
            var removing = new[] { "Carmack", "Palmer" };

            hashSet.PropertyChanging += (s, a) => AssertCountChanging(hashSet, s, a, currentCount, ref countChanging);
            hashSet.PropertyChanged += (s, a) => AssertCountChanged(hashSet, s, a, ref currentCount, countChange, ref countChanged);
            hashSet.CollectionChanged += (s, a) =>
                {
                    Assert.Equal(NotifyCollectionChangedAction.Replace, a.Action);
                    Assert.Equal(removing, a.OldItems.OfType<string>().OrderBy(i => i));
                    Assert.Empty(a.NewItems);
                    collectionChanged++;
                };

            hashSet.ExceptWith(new[] { "Carmack", "Palmer", "Abrash" });

            Assert.Equal(1, countChanging);
            Assert.Equal(1, countChanged);
            Assert.Equal(1, collectionChanged);
            Assert.Equal(new[] { "Brendan", "Nate" }, hashSet.OrderBy(i => i));

            hashSet.ExceptWith(new[] { "Abrash", "Carmack", "Palmer" });

            Assert.Equal(1, countChanging);
            Assert.Equal(1, countChanged);
            Assert.Equal(1, collectionChanged);
            Assert.Equal(new[] { "Brendan", "Nate" }, hashSet.OrderBy(i => i));
        }

        [Fact]
        public void Can_symetrical_except_with()
        {
            var hashSet = new ObservableHashSet<string> { "Brendan", "Carmack", "Nate", "Palmer" };
            var countChanging = 0;
            var countChanged = 0;
            var collectionChanged = 0;
            var currentCount = 4;
            var countChange = -1;
            var removing = new[] { "Carmack", "Palmer" };
            var adding = new[] { "Abrash" };

            hashSet.PropertyChanging += (s, a) => AssertCountChanging(hashSet, s, a, currentCount, ref countChanging);
            hashSet.PropertyChanged += (s, a) => AssertCountChanged(hashSet, s, a, ref currentCount, countChange, ref countChanged);
            hashSet.CollectionChanged += (s, a) =>
                {
                    Assert.Equal(NotifyCollectionChangedAction.Replace, a.Action);
                    Assert.Equal(removing, a.OldItems.OfType<string>().OrderBy(i => i));
                    Assert.Equal(adding, a.NewItems.OfType<string>().OrderBy(i => i));
                    collectionChanged++;
                };

            hashSet.SymmetricExceptWith(new[] { "Carmack", "Palmer", "Abrash" });

            Assert.Equal(1, countChanging);
            Assert.Equal(1, countChanged);
            Assert.Equal(1, collectionChanged);
            Assert.Equal(new[] { "Abrash", "Brendan", "Nate" }, hashSet.OrderBy(i => i));

            hashSet.SymmetricExceptWith(new string[0]);

            Assert.Equal(1, countChanging);
            Assert.Equal(1, countChanged);
            Assert.Equal(1, collectionChanged);
            Assert.Equal(new[] { "Abrash", "Brendan", "Nate" }, hashSet.OrderBy(i => i));
        }

        [Fact]
        public void IsSubsetOf_works_like_normal_hashset()
        {
            var bigData = CreateTestData();
            var smallData = CreateTestData(10);

            Assert.Equal(
                new HashSet<int>(smallData).IsSubsetOf(bigData),
                new ObservableHashSet<int>(smallData).IsSubsetOf(bigData));
        }

        [Fact]
        public void IsProperSubsetOf_works_like_normal_hashset()
        {
            var bigData = CreateTestData();
            var smallData = CreateTestData(10);

            Assert.Equal(
                new HashSet<int>(smallData).IsProperSubsetOf(bigData),
                new ObservableHashSet<int>(smallData).IsProperSubsetOf(bigData));
        }

        [Fact]
        public void IsSupersetOf_works_like_normal_hashset()
        {
            var bigData = CreateTestData();
            var smallData = CreateTestData(10);

            Assert.Equal(
                new HashSet<int>(bigData).IsSupersetOf(smallData),
                new ObservableHashSet<int>(bigData).IsSupersetOf(smallData));
        }

        [Fact]
        public void IsProperSupersetOf_works_like_normal_hashset()
        {
            var bigData = CreateTestData();
            var smallData = CreateTestData(10);

            Assert.Equal(
                new HashSet<int>(bigData).IsProperSupersetOf(smallData),
                new ObservableHashSet<int>(bigData).IsProperSupersetOf(smallData));
        }

        [Fact]
        public void Overlaps_works_like_normal_hashset()
        {
            var bigData = CreateTestData();
            var smallData = CreateTestData(10);

            Assert.Equal(
                new HashSet<int>(bigData).Overlaps(smallData),
                new ObservableHashSet<int>(bigData).Overlaps(smallData));
        }

        [Fact]
        public void SetEquals_works_like_normal_hashset()
        {
            var data1 = CreateTestData(5);
            var data2 = CreateTestData(5);

            Assert.Equal(
                new HashSet<int>(data1).SetEquals(data2),
                new ObservableHashSet<int>(data1).SetEquals(data2));
        }

        [Fact]
        public void TrimExcess_doesnt_throw()
        {
            var bigData = CreateTestData();
            var smallData = CreateTestData(10);

            var hashSet = new ObservableHashSet<int>(bigData.Concat(smallData));
            foreach (var item in bigData)
            {
                hashSet.Remove(item);
            }

            hashSet.TrimExcess();
        }

        [Fact]
        public void Can_remove_with_predicate()
        {
            var hashSet = new ObservableHashSet<string> { "Brendan", "Carmack", "Nate", "Palmer" };
            var countChanging = 0;
            var countChanged = 0;
            var collectionChanged = 0;
            var currentCount = 4;
            var countChange = -2;
            var removing = new[] { "Carmack", "Palmer" };

            hashSet.PropertyChanging += (s, a) => AssertCountChanging(hashSet, s, a, currentCount, ref countChanging);
            hashSet.PropertyChanged += (s, a) => AssertCountChanged(hashSet, s, a, ref currentCount, countChange, ref countChanged);
            hashSet.CollectionChanged += (s, a) =>
                {
                    Assert.Equal(NotifyCollectionChangedAction.Replace, a.Action);
                    Assert.Equal(removing, a.OldItems.OfType<string>().OrderBy(i => i));
                    Assert.Empty(a.NewItems);
                    collectionChanged++;
                };

            Assert.Equal(2, hashSet.RemoveWhere(i => i.Contains("m")));

            Assert.Equal(1, countChanging);
            Assert.Equal(1, countChanged);
            Assert.Equal(1, collectionChanged);
            Assert.Equal(new[] { "Brendan", "Nate" }, hashSet.OrderBy(i => i));

            Assert.Equal(0, hashSet.RemoveWhere(i => i.Contains("m")));

            Assert.Equal(1, countChanging);
            Assert.Equal(1, countChanged);
            Assert.Equal(1, collectionChanged);
            Assert.Equal(new[] { "Brendan", "Nate" }, hashSet.OrderBy(i => i));
        }

#if NET46

        [Fact]
        public void The_BindingList_returned_from_ObservableHasSet_is_cached()
        {
            var ols = new ObservableHashSet<string>();
            var bindingList = ols.ToBindingList();

            Assert.Same(bindingList, ols.ToBindingList());
        }

        [Fact]
        public void ToBindingList_returns_a_new_binding_list_each_time_when_called_on_non_DbLocalView_ObervableCollections()
        {
            var oc = new ObservableCollection<string>();

            var bindingList = oc.ToBindingList();
            Assert.NotNull(bindingList);

            var bindingListAgain = oc.ToBindingList();
            Assert.NotNull(bindingListAgain);
            Assert.NotSame(bindingList, bindingListAgain);
        }
#elif NETCOREAPP2_0
#else
#error target frameworks need to be updated.
#endif

        private static void AssertCountChanging<T>(
            ObservableHashSet<T> hashSet,
            object sender,
            PropertyChangingEventArgs eventArgs,
            int expectedCount,
            ref int changingCount)
        {
            Assert.Same(hashSet, sender);
            Assert.Equal("Count", eventArgs.PropertyName);
            Assert.Equal(expectedCount, hashSet.Count);
            changingCount++;
        }

        private static void AssertCountChanged<T>(
            ObservableHashSet<T> hashSet,
            object sender,
            PropertyChangedEventArgs eventArgs,
            ref int expectedCount,
            int countDelta,
            ref int changedCount)
        {
            Assert.Same(hashSet, sender);
            Assert.Equal("Count", eventArgs.PropertyName);
            Assert.Equal(expectedCount + countDelta, hashSet.Count);
            changedCount++;
            expectedCount += countDelta;
        }

        private static List<int> CreateTestData(int minSize = 0, int maxLength = 1000)
        {
            var length = _random.Next(minSize, maxLength);
            var data = new List<int>();
            for (var i = 0; i < length; i++)
            {
                data.Add(_random.Next(int.MinValue, int.MaxValue));
            }

            return data;
        }
    }
}
