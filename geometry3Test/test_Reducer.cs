﻿using System;
using System.Diagnostics;
using g4;

namespace geometry3Test 
{
	public static class test_Reducer 
	{
		public static bool WriteDebugMeshes = true;



		public static void test_basic_closed_reduce() {
			//DMesh3 mesh = TestUtil.MakeCappedCylinder(false);
			//DMesh3 mesh = TestUtil.LoadTestInputMesh("sphere_bowtie_groups.obj");
			DMesh3 mesh = TestUtil.LoadTestInputMesh("bunny_solid.obj");

			//MeshUtil.ScaleMesh(mesh, Frame3f.Identity, new Vector3f(1,2,1));
			//DMesh3 mesh = TestUtil.MakeOpenCylinder(false);
			mesh.CheckValidity();

			if ( WriteDebugMeshes )
				TestUtil.WriteTestOutputMesh(mesh, "basic_closed_reduce_before.obj");

			Reducer r = new Reducer(mesh);

			DMeshAABBTree3 tree = new DMeshAABBTree3(new DMesh3(mesh));
			tree.Build();
			//r.SetProjectionTarget(new MeshProjectionTarget() { Mesh = tree.Mesh, Spatial = tree });

			r.ReduceToTriangleCount(3000);
			//r.ReduceToEdgeLength(2.0);

			double mine, maxe, avge;
			MeshQueries.EdgeLengthStats(mesh, out mine, out maxe, out avge);
			System.Console.WriteLine("Edge length stats: {0} {1} {2}", mine, maxe, avge);

			if ( WriteDebugMeshes )
				TestUtil.WriteTestOutputMesh(mesh, "basic_closed_reduce_after.obj");
		}



        public static void test_reduce_constraints_fixedverts()
        {
            int Slices = 128;
			DMesh3 mesh = TestUtil.MakeCappedCylinder(false, Slices);
			MeshUtil.ScaleMesh(mesh, Frame3f.Identity, new Vector3f(1,2,1));
			mesh.CheckValidity();
            AxisAlignedBox3d bounds = mesh.CachedBounds;

            // construct mesh projection target
            DMesh3 meshCopy = new DMesh3(mesh);
            meshCopy.CheckValidity();
            DMeshAABBTree3 tree = new DMeshAABBTree3(meshCopy);
            tree.Build();
            MeshProjectionTarget target = new MeshProjectionTarget() {
                Mesh = meshCopy, Spatial = tree
            };

            if ( WriteDebugMeshes )
				TestUtil.WriteTestOutputMesh(mesh, "reduce_fixed_constraints_test_before.obj");

            // construct constraint set
            MeshConstraints cons = new MeshConstraints();

			//EdgeRefineFlags useFlags = EdgeRefineFlags.NoCollapse;
			EdgeRefineFlags useFlags = EdgeRefineFlags.PreserveTopology;

            foreach ( int eid in mesh.EdgeIndices() ) {
                double fAngle = MeshUtil.OpeningAngleD(mesh, eid);
                if (fAngle > 30.0f) {
					cons.SetOrUpdateEdgeConstraint(eid, new EdgeConstraint(useFlags) {TrackingSetID = 1});
                    Index2i ev = mesh.GetEdgeV(eid);
                    int nSetID0 = (mesh.GetVertex(ev[0]).y > bounds.Center.y) ? 1 : 2;
                    int nSetID1 = (mesh.GetVertex(ev[1]).y > bounds.Center.y) ? 1 : 2;
                    cons.SetOrUpdateVertexConstraint(ev[0], new VertexConstraint(true, nSetID0));
                    cons.SetOrUpdateVertexConstraint(ev[1], new VertexConstraint(true, nSetID1));
                }
            }

			Reducer r = new Reducer(mesh);
            r.SetExternalConstraints(cons);
            r.SetProjectionTarget(target);

			r.ReduceToTriangleCount(50);
			mesh.CheckValidity();

            if ( WriteDebugMeshes )
                TestUtil.WriteTestOutputMesh(mesh, "reduce_fixed_constraints_test_after.obj");
        }





        

		public static void test_reduce_profiling() {


            LocalProfiler p = new LocalProfiler();

            p.Start("load");
            //DMesh3 mesh = TestUtil.LoadTestMesh("c:\\scratch\\bunny_solid.obj");
            //DMesh3 mesh = TestUtil.LoadTestMesh("C:\\scratch\\current_test\\g3sharp_user_OBJ\\OBJ\\dizhi.obj");
            //DMesh3 mesh = TestUtil.LoadTestMesh("C:\\scratch\\current_test\\g3sharp_user_OBJ\\exported.obj");
            //DMesh3 mesh = TestUtil.LoadTestMesh("c:\\scratch\\bunny_open.obj");
            DMesh3 loadMesh = TestUtil.LoadTestMesh("c:\\scratch\\ZentrigDoo_Hires_Upper.stl");
            System.Console.WriteLine("Loaded...");

            p.StopAllAndStartNew("check");
			//mesh.CheckValidity();
            System.Console.WriteLine("Checked...");


            double time_ticks = 0;
            int Niters = 10;

            DMesh3 mesh = null;
            for (int k = 0; k < Niters; ++k) {

                mesh = new DMesh3(loadMesh);

                int N = 100000;
                System.Console.WriteLine("Reducing from {0} to {1}...", mesh.TriangleCount, N);
                BlockTimer reduceT = p.StopAllAndStartNew("reduce");
                Reducer r = new Reducer(mesh);
                //r.MinimizeQuadricPositionError = false;
                r.ENABLE_PROFILING = true;

                //DMeshAABBTree3 tree = new DMeshAABBTree3(new DMesh3(mesh));
                //tree.Build();
                //MeshProjectionTarget target = new MeshProjectionTarget(tree.Mesh, tree);
                //r.SetProjectionTarget(target);
                //r.ProjectionMode = Reducer.TargetProjectionMode.Inline;

                //r.SetExternalConstraints(new MeshConstraints());
                ////MeshConstraintUtil.PreserveBoundaryLoops(r.Constraints, mesh);
                //MeshConstraintUtil.FixAllBoundaryEdges(r.Constraints, mesh);

                r.ReduceToTriangleCount(N);
                //double min, max, avg;
                //MeshQueries.EdgeLengthStats(mesh, out min, out max, out avg);
                //r.ReduceToEdgeLength(avg * 1.5);
                //System.Console.WriteLine("Reduced...");

                p.Stop("reduce");
                time_ticks += reduceT.Watch.Elapsed.Ticks;
                System.Console.WriteLine(p.AllTimes());
                GC.Collect();
            }


            TimeSpan total = new TimeSpan((int)(time_ticks / (double)Niters));
            System.Console.WriteLine("AVERAGE: {0}", string.Format("{0:ss}.{0:ffffff}", total));

            TestUtil.WriteDebugMesh(mesh, "__REDUCED.obj");
		}




	}
}
