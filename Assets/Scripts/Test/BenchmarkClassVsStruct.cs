using System;

using UnityEngine;

class BenchmarkClassVsStruct : MonoBehaviour
{
    [SerializeField] bool shuffleArrays = true;

    struct ProjectileStruct
    {
        public Vector3 Position;
        public Vector3 Velocity;
    }

    class ProjectileClass
    {
        public Vector3 Position;
        public Vector3 Velocity;
    }

    void Start()
    {
        const int count = 10000000;
        ProjectileStruct[] projectileStructs = new ProjectileStruct[count];
        ProjectileClass[] projectileClasses = new ProjectileClass[count];

        System.Collections.Generic.List<ProjectileStruct> structListTest = new System.Collections.Generic.List<ProjectileStruct>();
        structListTest.Add(projectileStructs[0]);
        structListTest.Add(projectileStructs[1]);
        Debug.Log(structListTest.Capacity);

        for (int i = 0; i < count; ++i)
        {
            projectileClasses[i] = new ProjectileClass();
        }

        if (shuffleArrays)
        {
            Shuffle(projectileStructs);
            Shuffle(projectileClasses);
        }

        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < count; ++i)
        {
            UpdateProjectile(ref projectileStructs[i], 0.5f);
        }
        long structTime = sw.ElapsedMilliseconds;

        sw.Reset();
        sw.Start();
        for (int i = 0; i < count; ++i)
        {
            UpdateProjectile(projectileClasses[i], 0.5f);
        }
        long classTime = sw.ElapsedMilliseconds;

        Debug.Log($"TIME - STRUCT={structTime},CLASS={classTime}");
    }

    void UpdateProjectile(ref ProjectileStruct projectile, float time)
    {
        projectile.Position += projectile.Velocity * time;
    }

    void UpdateProjectile(ProjectileClass projectile, float time)
    {
        projectile.Position += projectile.Velocity * time;
    }

    public static void Shuffle<T>(T[] list)
    {
        System.Random random = new System.Random();
        for (int n = list.Length; n > 1;)
        {
            n--;
            int k = random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}