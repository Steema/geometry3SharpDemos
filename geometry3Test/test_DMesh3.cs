﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using g4;

namespace geometry3Test
{
    public static class test_DMesh3
    {
        public static void basic_tests()
        {
            System.Console.WriteLine("DMesh3:basic_tests() starting");

            DMesh3 tmp = new DMesh3();
            CappedCylinderGenerator cylgen = new CappedCylinderGenerator();
            cylgen.Generate();
            cylgen.MakeMesh(tmp);
            tmp.CheckValidity();

            System.Console.WriteLine("cylinder ok");
        }


        public static void test_normals()
        {
            // check that frames are ok
            DMesh3 mesh = TestUtil.LoadTestInputMesh("bunny_solid.obj");
            foreach (int tid in mesh.TriangleIndices()) {
                Vector3f n = (Vector3f)mesh.GetTriNormal(tid);
                for (int j = 0; j < 3; ++j) {
                    Frame3f f = mesh.GetTriFrame(tid, j);
                    if (Math.Abs(f.X.Dot(f.Y)) > MathUtil.ZeroTolerancef ||
                         Math.Abs(f.X.Dot(f.Z)) > MathUtil.ZeroTolerancef ||
                         Math.Abs(f.Y.Dot(f.Z)) > MathUtil.ZeroTolerancef)
                        throw new Exception("argh");
                    Vector3f fn = f.Z;
                    if (fn.Dot(n) < 0.99)
                        throw new Exception("shit");
                }
            }

            MeshNormals.QuickCompute(mesh);

            foreach (int vid in mesh.VertexIndices()) {
                Vector3f n = mesh.GetVertexNormal(vid);
                for (int j = 1; j <= 2; ++j) {
                    Frame3f f = mesh.GetVertexFrame(vid, (j == 1) ? true : false);
                    Vector3f fn = f.GetAxis(j);
                    if (Math.Abs(f.X.Dot(f.Y)) > MathUtil.ZeroTolerancef ||
                         Math.Abs(f.X.Dot(f.Z)) > MathUtil.ZeroTolerancef ||
                         Math.Abs(f.Y.Dot(f.Z)) > MathUtil.ZeroTolerancef)
                        throw new Exception("argh");
                    if (fn.Dot(n) < 0.99)
                        throw new Exception("shit2");
                }
            }

        }





        public static void test_remove()
        {
            System.Console.WriteLine("DMesh3:test_remove() starting");

            List<DMesh3> testMeshes = new List<DMesh3>() {
                TestUtil.MakeTrivialRect(),
                TestUtil.MakeOpenCylinder(false),       // removing any creates bowtie!
                TestUtil.MakeOpenCylinder(true),
                TestUtil.MakeCappedCylinder(false),
                TestUtil.MakeCappedCylinder(true)
            };

            // remove-one tests
            foreach (DMesh3 mesh in testMeshes) {
                int N = mesh.TriangleCount;
                for (int j = 0; j < N; ++j) {
                    DMesh3 r1 = new DMesh3(mesh);
                    r1.RemoveTriangle(j, false);
                    r1.CheckValidity(true);         // remove might create non-manifold tris at bdry

                    DMesh3 r2 = new DMesh3(mesh);
                    r2.RemoveTriangle(j, true);
                    r2.CheckValidity(true);

                    DMesh3 r3 = new DMesh3(mesh);
                    r3.RemoveTriangle(j, false, true);
                    r3.CheckValidity(false);         // remove might create non-manifold tris at bdry

                    DMesh3 r4 = new DMesh3(mesh);
                    r4.RemoveTriangle(j, true, true);
                    r4.CheckValidity(false);
                }
            }


            // grinder tests
            foreach ( DMesh3 mesh in testMeshes ) {

                // sequential
                DMesh3 tmp = new DMesh3(mesh);
                bool bDone = false;
                while (!bDone) {
                    bDone = true;
                    foreach ( int ti in tmp.TriangleIndices() ) {
                        if ( tmp.IsTriangle(ti) && tmp.RemoveTriangle(ti, true, true) == MeshResult.Ok ) {
                            bDone = false;
                            tmp.CheckValidity(false);
                        }
                    }
                }
                System.Console.WriteLine(string.Format("remove_all sequential: before {0} after {1}", mesh.TriangleCount, tmp.TriangleCount));

                // randomized
                tmp = new DMesh3(mesh);
                bDone = false;
                while (!bDone) {
                    bDone = true;
                    foreach ( int ti in tmp.TriangleIndices() ) {
                        int uset = (ti + 256) % tmp.MaxTriangleID;        // break symmetry
                        if ( tmp.IsTriangle(uset) && tmp.RemoveTriangle(uset, true, true) == MeshResult.Ok ) {
                            bDone = false;
                            tmp.CheckValidity(false);
                        }
                    }
                }
                System.Console.WriteLine(string.Format("remove_all randomized: before {0} after {1}", mesh.TriangleCount, tmp.TriangleCount));
            }


            System.Console.WriteLine("remove ok");
        }



		public static void split_tests(bool bTestBoundary, int N = 100) {
			System.Console.WriteLine("DMesh3:split_tests() starting");

			DMesh3 mesh = TestUtil.MakeCappedCylinder(bTestBoundary);
			mesh.CheckValidity();

			Random r = new Random(31377);
			for ( int k = 0; k < N; ++k ) {
				int eid = r.Next() % mesh.EdgeCount;
				if ( ! mesh.IsEdge(eid) )
					continue;

				DMesh3.EdgeSplitInfo splitInfo; 
				MeshResult result = mesh.SplitEdge(eid, out splitInfo);
				Debug.Assert(result == MeshResult.Ok);
				mesh.CheckValidity();
			}

			System.Console.WriteLine("splits ok");
		}


		public static void flip_tests(bool bTestBoundary, int N = 100) {
			System.Console.WriteLine("DMesh3:flip_tests() starting");

			DMesh3 mesh = TestUtil.MakeCappedCylinder(bTestBoundary);
			mesh.CheckValidity();

			Random r = new Random(31377);
			for ( int k = 0; k < N; ++k ) {
				int eid = r.Next() % mesh.EdgeCount;
				if ( ! mesh.IsEdge(eid) )
					continue;
				bool bBoundary = mesh.IsBoundaryEdge(eid);

				DMesh3.EdgeFlipInfo flipInfo; 
				MeshResult result = mesh.FlipEdge(eid, out flipInfo);
				if ( bBoundary )
					Debug.Assert(result == MeshResult.Failed_IsBoundaryEdge);
				else
					Debug.Assert(result == MeshResult.Ok || result == MeshResult.Failed_FlippedEdgeExists);
				mesh.CheckValidity();
			}

			System.Console.WriteLine("flips ok");
		}



		public static void collapse_tests(bool bTestBoundary, int N = 100) {

			bool write_debug_meshes = false;

			DMesh3 mesh = TestUtil.MakeCappedCylinder(bTestBoundary);
			mesh.CheckValidity();

			System.Console.WriteLine( string.Format("DMesh3:collapse_tests() starting - test bdry {2}, verts {0} tris {1}", 
			                                        mesh.VertexCount, mesh.TriangleCount, bTestBoundary) );

			if(write_debug_meshes)
				TestUtil.WriteDebugMesh(mesh, string.Format("before_collapse_{0}.obj", ((bTestBoundary)?"boundary":"noboundary")));


			Random r = new Random(31377);
			for ( int k = 0; k < N; ++k ) {
				int eid = r.Next() % mesh.EdgeCount;
				if ( ! mesh.IsEdge(eid) )
					continue;
				//bool bBoundary = mesh.IsBoundaryEdge(eid);
				//if (bTestBoundary && bBoundary == false)
				//	 continue;
				Index2i ev = mesh.GetEdgeV(eid);

				DMesh3.EdgeCollapseInfo collapseInfo; 
				MeshResult result = mesh.CollapseEdge(ev[0], ev[1], out collapseInfo);
				Debug.Assert(
					result != MeshResult.Failed_NotAnEdge &&
					result != MeshResult.Failed_FoundDuplicateTriangle );

				mesh.CheckValidity();
			}

			System.Console.WriteLine( string.Format("random collapses ok - verts {0} tris {1}", 
			                                        mesh.VertexCount, mesh.TriangleCount) );


			collapse_to_convergence(mesh);

			System.Console.WriteLine( string.Format("all possible collapses ok - verts {0} tris {1}", 
			                                        mesh.VertexCount, mesh.TriangleCount) );

			if(write_debug_meshes)
				TestUtil.WriteDebugMesh(mesh, string.Format("after_collapse_{0}.obj", ((bTestBoundary)?"boundary":"noboundary")));
		}




		// this function collapses edges until it can't anymore
		static void collapse_to_convergence(DMesh3 mesh) {
			bool bContinue = true;
			while (bContinue) {
				bContinue = false;
				for ( int eid = 0; eid < mesh.MaxEdgeID; ++eid) { 
					if ( ! mesh.IsEdge(eid) )
						continue;
					Index2i ev = mesh.GetEdgeV(eid);
					DMesh3.EdgeCollapseInfo collapseInfo; 
					MeshResult result = mesh.CollapseEdge(ev[0], ev[1], out collapseInfo);
					if ( result == MeshResult.Ok ) {
						bContinue = true;
						break;
					}
				}

			}
		}





		// cyl with no shared verts should collapse down to two triangles
		public static void collapse_test_convergence_cyl_noshared() {
			DMesh3 mesh = TestUtil.MakeCappedCylinder(true);
			mesh.CheckValidity();
			collapse_to_convergence(mesh);
			Util.gDevAssert( mesh.TriangleCount == 3 );
			Util.gDevAssert( mesh.VertexCount == 9 );
			foreach ( int tid in mesh.TriangleIndices() )
				Util.gDevAssert( mesh.tri_is_boundary(tid) );
		}

		// open cylinder (ie a tube) should collapse down to having two boundary loops with 3 verts/edges each
		public static void collapse_test_convergence_opencyl() {
			DMesh3 mesh = TestUtil.MakeOpenCylinder(false);
			mesh.CheckValidity();

			collapse_to_convergence(mesh);
			int bdry_v = 0, bdry_t = 0, bdry_e = 0;
			foreach ( int eid in mesh.EdgeIndices() ) {
				if ( mesh.IsBoundaryEdge(eid) )
					bdry_e++;
			}
			Util.gDevAssert(bdry_e == 6);
			foreach ( int tid in mesh.TriangleIndices() ) {
				if ( mesh.tri_is_boundary(tid) )
					bdry_t++;
			}
			Util.gDevAssert(bdry_t == 6);			
			foreach ( int vid in mesh.VertexIndices() ) {
				if ( mesh.IsBoundaryVertex(vid) )
					bdry_v++;
			}
			Util.gDevAssert(bdry_v == 6);					
		}

		// closed mesh should collapse to a tetrahedron
		public static void collapse_test_closed_mesh() {
			DMesh3 mesh = TestUtil.MakeCappedCylinder(false);
			mesh.CheckValidity();
			collapse_to_convergence(mesh);
            mesh.CheckValidity();
			Util.gDevAssert( mesh.TriangleCount == 4 );
			Util.gDevAssert( mesh.VertexCount == 4 );
			foreach ( int eid in mesh.EdgeIndices() )
				Util.gDevAssert( mesh.IsBoundaryEdge(eid) == false );
		}







		public static void merge_test_closed_mesh()
		{
			DMesh3 mesh = TestUtil.MakeCappedCylinder(true, 4);
			mesh.CheckValidity();

			DMesh3.MergeEdgesInfo info;

			int merges = 0;
			while (true) {
				List<int> be = new List<int>(mesh.BoundaryEdgeIndices());
				if (be.Count == 0)
					break;
				int ea = be[0];
				int eo = find_pair_edge(mesh, ea, be);
				if (eo != DMesh3.InvalidID) {
					var result = mesh.MergeEdges(ea, eo, out info);
					Util.gDevAssert(result == MeshResult.Ok);
					TestUtil.WriteTestOutputMesh(mesh, "after_last_merge.obj");
					mesh.CheckValidity();
					merges++;
				}
			}
			mesh.CheckValidity();


			DMesh3 originalMesh = TestUtil.LoadTestInputMesh("three_edge_crack.obj");
			List<int> bdryedges = new List<int>(originalMesh.BoundaryEdgeIndices());
			for (int k = 0; k < bdryedges.Count; ++k) {
				DMesh3 copyMesh = new DMesh3(originalMesh);
				List<int> be = new List<int>(copyMesh.BoundaryEdgeIndices());
				int ea = be[k];
				int eo = find_pair_edge(copyMesh, ea, be);
				if (eo != DMesh3.InvalidID) {
					var result = copyMesh.MergeEdges(ea, eo, out info);
					Util.gDevAssert(result == MeshResult.Ok);
					if ( k == 3 )
						TestUtil.WriteTestOutputMesh(copyMesh, "after_last_merge.obj");
					mesh.CheckValidity();
				}
			}

			// this should fail at every edge because it would create bad-orientation edges
			DMesh3 dupeMesh = TestUtil.LoadTestInputMesh("duplicate_4tris.obj");
			List<int> dupeBE = new List<int>(dupeMesh.BoundaryEdgeIndices());
			for (int k = 0; k < dupeBE.Count; ++k) {
				int ea = dupeBE[k];
				int eo = find_pair_edge(dupeMesh, ea, dupeBE);
				if (eo != DMesh3.InvalidID) {
					var result = dupeMesh.MergeEdges(ea, eo, out info);
					Util.gDevAssert(result == MeshResult.Failed_SameOrientation);
					mesh.CheckValidity();
					TestUtil.WriteTestOutputMesh(dupeMesh, "after_last_merge.obj");
				}
			}
		}

		static int find_pair_edge(DMesh3 mesh, int eid, List<int> candidates) {
			Index2i ev = mesh.GetEdgeV(eid);
			Vector3d a = mesh.GetVertex(ev.a), b = mesh.GetVertex(ev.b);
			double eps = 100 * MathUtil.Epsilonf;
			foreach (int eother in candidates ) {
				if (eother == eid)
					continue;
				Index2i ov = mesh.GetEdgeV(eother);
				Vector3d c = mesh.GetVertex(ov.a), d = mesh.GetVertex(ov.b);
				if ((a.EpsilonEqual(c, eps) && b.EpsilonEqual(d, eps)) ||
				    (b.EpsilonEqual(c, eps) && a.EpsilonEqual(d, eps)))
					return eother;
			};
			return DMesh3.InvalidID;
		}










        public static void poke_test()
        {
            DMesh3 mesh = TestUtil.LoadTestInputMesh("plane_250v.obj");
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("sphere_bowtie_groups.obj");
            mesh.CheckValidity();


            int NT = mesh.TriangleCount;
            for ( int i = 0; i < NT; i += 5 ) {
                Vector3d n = mesh.GetTriNormal(i);
                DMesh3.PokeTriangleInfo pokeinfo;
                MeshResult result = mesh.PokeTriangle(i, out pokeinfo);

                Vector3d v = mesh.GetVertex(pokeinfo.new_vid);
                v += 0.25f * n;
                mesh.SetVertex(pokeinfo.new_vid, v);

                mesh.CheckValidity();
            }

            //TestUtil.WriteTestOutputMesh(mesh, "poke_test_result.obj");

        }





        public static void set_triangle_tests()
        {
            DMesh3 mesh = TestUtil.LoadTestInputMesh("plane_250v.obj");
            mesh.CheckValidity();

            // ok todo


        }





        public static void copy_performance()
        {
            Sphere3Generator_NormalizedCube meshgen = new Sphere3Generator_NormalizedCube() { EdgeVertices = 200 };
            meshgen.Generate();
            DMesh3 sphereMesh = meshgen.MakeDMesh();

            DateTime start = DateTime.Now;

            for (int k = 0; k < 250; ++k) {
                if (k % 10 == 0)
                    System.Console.WriteLine("{0} / 250", k);
                DMesh3 m = new DMesh3(sphereMesh);
                //m.CheckValidity();
                //if (!m.IsSameMesh(sphereMesh))
                //    System.Console.WriteLine("NOT SAME MESH!");
            }

            DateTime end = DateTime.Now;
            System.Console.WriteLine("Time {0}", (end - start).TotalSeconds);
        }






        public static void test_compact_in_place()
        {
            DMesh3 testMesh = TestUtil.LoadTestInputMesh("bunny_solid.obj");
            testMesh.CheckValidity();
            int[] test_counts = new int[] { 16, 256, 1023, 1024, 1025, 2047, 2048, 2049, testMesh.TriangleCount - 1, testMesh.TriangleCount };
            foreach (int count in test_counts) {
                DMesh3 mesh = new DMesh3(testMesh);
                Reducer r = new Reducer(mesh); r.ReduceToTriangleCount(count);
                mesh.CompactInPlace();
                mesh.CheckValidity(false, FailMode.DebugAssert);
                Util.gDevAssert(mesh.IsCompact);
            }
        }




        public static void test_insert()
        {
            DMesh3 testMesh = new DMesh3();

            if (testMesh.InsertVertex(10, new NewVertexInfo(Vector3d.Zero)) != MeshResult.Ok)
                Console.WriteLine("FAILED testMesh.InsertVertex(10)");
            if (testMesh.InsertVertex(11, new NewVertexInfo(Vector3d.Zero)) != MeshResult.Ok)
                Console.WriteLine("FAILED testMesh.InsertVertex(11)");
            if (testMesh.InsertVertex(5000, new NewVertexInfo(Vector3d.Zero)) != MeshResult.Ok)
                Console.WriteLine("FAILED testMesh.InsertVertex(5000)");
            if (testMesh.InsertVertex(1000, new NewVertexInfo(Vector3d.Zero)) != MeshResult.Ok)
                Console.WriteLine("FAILED testMesh.InsertVertex(1000)");

            if (testMesh.InsertTriangle(7, new Index3i(10, 11, 5000)) != MeshResult.Ok)
                Console.WriteLine("FAILED testMesh.InsertTriangle(7)");
            if (testMesh.InsertTriangle(2, new Index3i(11, 10, 1000)) != MeshResult.Ok)
                Console.WriteLine("FAILED testMesh.InsertTriangle(2)");

            foreach ( int vid in testMesh.VertexIndices() ) {
                List<int> edges = new List<int>(testMesh.VertexEdges.ValueItr(vid));
            }

            testMesh.CheckValidity(false, FailMode.DebugAssert);
        }


        public static void test_remove_change_apply()
        {
            DMesh3 testMesh = TestUtil.LoadTestInputMesh("bunny_solid.obj");
            DMesh3 copy = new DMesh3(testMesh);
            Vector3d c = testMesh.CachedBounds.Center;

            MeshFaceSelection selection = new MeshFaceSelection(testMesh);
            foreach (int tid in testMesh.TriangleIndices()) {
                if (testMesh.GetTriCentroid(tid).x > c.x)
                    selection.Select(tid);
            }

            RemoveTrianglesMeshChange change = new RemoveTrianglesMeshChange();
            change.InitializeFromApply(testMesh, selection);

            testMesh.CheckValidity(true);
            change.Apply(copy);
            copy.CheckValidity(true);

            if (!copy.IsSameMesh(testMesh, true))
                System.Console.WriteLine("FAILED copy.IsSameMesh() 1");

            change.Revert(testMesh);
            testMesh.CheckValidity(false);
            change.Revert(copy);
            copy.CheckValidity(false);

            if (!copy.IsSameMesh(testMesh, true))
                System.Console.WriteLine("FAILED copy.IsSameMesh() 1");

            System.Console.WriteLine("test_remove_change_apply ok");
        }



        public static void test_remove_change_construct()
        {
            DMesh3 testMesh = TestUtil.LoadTestInputMesh("bunny_open_base.obj");

            Random r = new Random(31337);
            //int N = 100;
            int N = 10;
            int[] indices = TestUtil.RandomIndices(N, r, testMesh.MaxVertexID);
            for (int ii = 0; ii < N; ++ii) {
                MeshFaceSelection selection = new MeshFaceSelection(testMesh);
                selection.SelectVertexOneRing(indices[ii]);
                selection.ExpandToOneRingNeighbours(8);

                RemoveTrianglesMeshChange change = new RemoveTrianglesMeshChange();
                change.InitializeFromExisting(testMesh, selection);

                DMesh3 removed = new DMesh3(testMesh);
                MeshEditor.RemoveTriangles(removed, selection);

                DMesh3 changeCopy = new DMesh3(testMesh);
                change.Apply(changeCopy);
                changeCopy.CheckValidity(true);

                if (!changeCopy.IsSameMesh(removed, true))
                    System.Console.WriteLine("FAILED copy.IsSameMesh() 1");

                change.Revert(changeCopy);
                changeCopy.CheckValidity(false);

                if (!changeCopy.IsSameMesh(testMesh, true))
                    System.Console.WriteLine("FAILED copy.IsSameMesh() 1");
            }

            System.Console.WriteLine("test_remove_change_construct ok");
        }




        public static void test_add_change()
        {
            DMesh3 testMesh = TestUtil.LoadTestInputMesh("bunny_open_base.obj");
            DMesh3 copy = new DMesh3(testMesh);

            MeshBoundaryLoops loops = new MeshBoundaryLoops(copy);
            foreach ( var loop in loops ) {
                SimpleHoleFiller filler = new SimpleHoleFiller(copy, loop);
                bool ok = filler.Fill();
                Util.gDevAssert(ok);

                AddTrianglesMeshChange change = new AddTrianglesMeshChange();
                change.InitializeFromExisting(copy,
                    new List<int>() { filler.NewVertex }, filler.NewTriangles);

                DMesh3 tmp = new DMesh3(copy);
                change.Revert(copy);
                copy.CheckValidity(true);

                if (!copy.IsSameMesh(testMesh, true))
                    System.Console.WriteLine("FAILED copy.IsSameMesh() 1");

                change.Apply(copy);
                copy.CheckValidity(true);
                if (!copy.IsSameMesh(tmp, true))
                    System.Console.WriteLine("FAILED copy.IsSameMesh() 1");
            }

            System.Console.WriteLine("test_add_change ok");
        }





        public static void performance_grinder()
        {
            LocalProfiler p = new LocalProfiler();

            DateTime start = DateTime.Now;

            //p.Start("Meshgen");
            //for (int k = 0; k < 100; ++k) {
            //    Sphere3Generator_NormalizedCube tmpgen = new Sphere3Generator_NormalizedCube();
            //    tmpgen.EdgeVertices = 100;
            //    tmpgen.Generate();
            //    DMesh3 tmp = tmpgen.MakeDMesh();
            //}
            //p.StopAndAccumulate("Meshgen");

            //System.Console.WriteLine("done meshgen");

            Sphere3Generator_NormalizedCube meshgen = new Sphere3Generator_NormalizedCube() { EdgeVertices = 100 };
            meshgen.Generate();
            DMesh3 sphereMesh = meshgen.MakeDMesh();


            //p.Start("Spatial");
            //for (int k = 0; k < 100; ++k) {
            //    DMeshAABBTree3 tmpspatial = new DMeshAABBTree3(sphereMesh);
            //    tmpspatial.Build();
            //}
            //p.StopAndAccumulate("Spatial");

            //System.Console.WriteLine("done spatial");

            meshgen.EdgeVertices = 5;
            meshgen.Generate();
            sphereMesh = meshgen.MakeDMesh();
            double remesh_len = (2.0 / 5.0) * 0.025;   // takes ~220s
            //double remesh_len = (2.0 / 5.0) * 0.05;

            long max_mem = 0;
            Remesher remesher = new Remesher(sphereMesh);
            for (int k = 0; k < 10; ++k) {
                System.Console.WriteLine("{0}", k);
                p.Start("Remesh");
                remesher.SetTargetEdgeLength(remesh_len);
                remesher.SmoothSpeedT = 0.5f;
                for (int j = 0; j < 20; ++j) {
                    remesher.BasicRemeshPass();
                    foreach (int vid in sphereMesh.VertexIndices()) {
                        Vector3d v = sphereMesh.GetVertex(vid);
                        v.Normalize();
                        sphereMesh.SetVertex(vid, v);
                    }
                }
                p.StopAndAccumulate("Remesh");

                //System.Console.WriteLine(sphereMesh.MeshInfoString());

                System.Console.WriteLine(" {0}", k);

                p.Start("Reduce");
                remesher.SetTargetEdgeLength(remesh_len * 10);
                for (int j = 0; j < 20; ++j) {
                    remesher.BasicRemeshPass();
                    foreach (int vid in sphereMesh.VertexIndices()) {
                        Vector3d v = sphereMesh.GetVertex(vid);
                        v.Normalize();
                        sphereMesh.SetVertex(vid, v);
                    }
                }
                p.StopAndAccumulate("Reduce");
            }

            DateTime end = DateTime.Now;

            System.Console.WriteLine("done remesh");
            System.Console.WriteLine("Time {0} MaxMem {1}", (end-start).TotalSeconds, max_mem / (1024*1024));
            System.Console.WriteLine(p.AllAccumulatedTimes("Accumulated: "));
        }


    }
}
