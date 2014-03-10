
//using System;
using System.Collections.Generic;

using Sce.Pss.Core;
using Sce.Pss.Core.Environment;
using Sce.Pss.Core.Graphics;
using Sce.Pss.Core.Input;
using Sce.Pss.HighLevel.Physics2D;
using Sce.Pss.HighLevel.GameEngine2D;
using Sce.Pss.HighLevel.GameEngine2D.Base;

using TM = System.Int64;
namespace t0
{
	public class AppMain
	{
		private static GraphicsContext graphics;
		public static void Main (string[] args)
		{
			Initialize ();

			while (true) {
				SystemEvents.CheckEvents ();
				Update ();
				Render ();
			}
		}
		
		public struct Ip{
			public GamePadData gpd;
			public List<TouchData> td;
			public Vector2 vp2w;
			public float vp2w_s;
			public float dtf;
			public TM t;
			public int cur_frame;
			public float cur_frame_f;
			public System.Random sr;
		};
		
		public struct VC{//visualization context
			public TextureInfo ti_units,ti_prjs;
			public Camera2D cam;
			//public TextureInfo ti_pars
		};
		static VC vc;
		
		public struct AS{
			public bool atking;
			public TM lat;
		};
		const int max_nShields=4;
		public struct Dm{
			public float hp;
			public float hpm;
			public struct Sh{
				public float sp;
				public float spm;
				public float spr;
				public float ar;
			};
			public int sh_start_id;
			public int sh_count;
			//Sh[max_nShields];
		};
		public struct PC{
			public struct CS{
				public enum T{ N,D,W };
				public T tag;
				public struct _N{
					public int history;
					public int lastPressTime;
				};
			};
			public CS cs;
			public RBWS rbws;
			public struct PS{
				public enum T{ N,WPre,WPost,C/*casting*/ };
				public T tag;
				public struct _N{
					public AS atks;
				};
				public _N _n;
				public struct _WPre{
					public TM t_start;
					public Vector2 target;
				};
				public _WPre _wpre;
				public struct _WPost{
					public TM t_start;
				};
				public _WPost _wpost;
			};
			public PS ps;
			public Dm d;
			public Vector2 aim;//relative to 
		};
		public struct RBWS{
			//public PhysicsBody rb;
			//public PhysicsShape s;
			public struct RB{
				public Vector2 p;
				public Vector2 r;
				public Vector2 v;
				public float av;
				//public Vector2 r;/*right vector*/
			};
			public RB rb;
			public struct S{
			};
			public S s;
		};
		public struct NPC{
			public RBWS rbws;
			public struct PS{
				public AS atks;
			};
			public struct FV{
				public float b;//bigness
				public float r;//randomness
				public float a;//aggressiveness
			};
			public FV fv;
			public PS ps;
			public int fac;
			public Dm d;
			public int cid;
		};
		public struct Pj{
			public RBWS rbws;
			public int lb;// -1 for player
			public float dmg;
			public float r;
			public Vector2 sz;
			public int i;
			public TM t0,t1;
		}
		const int max_NPCs=1024;
		const int max_Pjs=1024;
		const int max_Shs=1024;
		
//		public struct W_Sc{
//			public Dictionary<int,Node> npcs,pjs;
//			public Node pc;
//		};
		const int nFacs=8;
		const int resFldsX=512,resFldsY=256;
		unsafe public struct W{
			public PC pc;
			//public NPC[] npcs=new NPC[max_NPCs];//[max_NPCs];
			public NPC[] npcs;
			//public Pj[] pjs=new Pj[max_Pjs];//[max_Pjs];
			public Pj[] pjs;
			public fixed int frm[nFacs*nFacs];
			public Dm.Sh[] shs;
			public LinkedList<int> 	active_npcs,	unused_npcs,
									active_pjs,		unused_pjs,
									active_shs,		unused_shs;
			public int lastSpawn;
			//public Dm.Sh[] shields=new Dm.Sh[max_Shs];
			public enum F{
				TIMESTAMP=0,
				DRAG=1,
				FORCE_X=2,
				FORCE_Y=3,
				DMG=4,
				F_TOT=5
			};
			public const int flds_length=resFldsY*resFldsX*(int)F.F_TOT;
			//public fixed short flds[flds_length];
			public float[] flds;
			const int y_step=resFldsX*(int)F.F_TOT;
			const int x_step=(int)F.F_TOT;
			public const float x_off=resFldsX/2,y_off=resFldsY/2;
			public const float x_cap=resFldsX-1.01f,y_cap=resFldsY-1.01f;
			public struct Sampler{
				public int ix0y0,ix0y1,ix1y0,ix1y1;
				public float dx,dy,dx0,dy0;
			};
			public float x_for_f(float x){ return System.Math.Min(System.Math.Max(0.0f,x+x_off),x_cap); }
			public float y_for_f(float y){ return System.Math.Min(System.Math.Max(0.0f,y+y_off),y_cap); }
			public float scale_for_f(float r){ return r; }
			public void GetSampler(float x,float y,out Sampler s){
				x=x_for_f (x);
				y=y_for_f (y);
				int x0=(int)x;
				int y0=(int)y;
				s.ix0y0=y0*y_step+x0*x_step;
				s.ix0y1=s.ix0y0+y_step;
				s.ix1y0=s.ix0y0+x_step;
				s.ix1y1=s.ix0y1+x_step;
				s.dx=x-(float)x0;
				s.dy=y-(float)y0;
				s.dx0=1.0f-s.dx;
				s.dy0=1.0f-s.dy;
				ReviewFld(s.ix0y0);
				ReviewFld(s.ix0y1);
				ReviewFld(s.ix1y0);
				ReviewFld(s.ix1y1);
			}
			public void ReviewFld(int iBase){
				if(flds[iBase]!=ip.cur_frame_f){
					flds[iBase]=ip.cur_frame_f;
					for(int k=1;k<(int)F.F_TOT;++k){
						flds[iBase+k]=0.0f;
					}
				}
			}
			public float Sample(ref Sampler s,int c){
				//fixed(float *b=flds){
					float x0y0=flds[s.ix0y0+c];
					float x0y1=flds[s.ix0y1+c];
					float x1y0=flds[s.ix1y0+c];
					float x1y1=flds[s.ix1y1+c];
					return 	(x0y0*s.dx0+x1y0*s.dx)*s.dy0
						+	(x0y1*s.dx0+x1y1*s.dx)*s.dy;
				//}
			}
			public void AddRadialForce(float x,float y,float r,float amp){
				x = x_for_f (x);
				y = y_for_f (y);
				r = scale_for_f(r);
				int x0 = (int)System.Math.Max (0,x-r);
				int x1 = (int)System.Math.Min (x+r,x_cap);
				int y0 = (int)System.Math.Max (0,y-r);
				int y1 = (int)System.Math.Min (y+r,y_cap);
				int i=y0*y_step+x0*x_step;
				float r2=r*r;
				float dx_ = (float)x0-x;
				float dy = (float)y0-y;
				for(int iy=y0;iy<=y1;++iy){
					int j=i;
					float dx = dx_;
					for(int ix=x0;ix<=x1;++ix){
						float dx2=dx*dx,dy2=dy*dy;
						float d2=dx2+dy2;
						if(d2<r2&&d2>=1.1f){
							ReviewFld(j);
							float coeff=(1.0f-d2/r2)/d2*amp;
							flds[j+(int)F.FORCE_X]+=dx2*coeff;
							flds[j+(int)F.FORCE_Y]+=dy2*coeff;
						}
						j+=x_step;
						dx+=1.0f;
					}
					dy+=1.0f;
					i+=y_step;
				}
			}
			public void AddRadialScalar(float x,float y,float r,float amp,int cmp){
				x = x_for_f (x);
				y = y_for_f (y);
				r = scale_for_f(r);
				int x0 = (int)System.Math.Max (0,x-r);
				int x1 = (int)System.Math.Min (x+r,x_cap);
				int y0 = (int)System.Math.Max (0,y-r);
				int y1 = (int)System.Math.Min (y+r,y_cap);
				int i=y0*y_step+x0*x_step;
				float r2=r*r;
				float dx_ = (float)x0-x;
				float dy = (float)y0-y;
				for(int iy=y0;iy<=y1;++iy){
					int j=i;
					float dx = dx_;
					for(int ix=x0;ix<=x1;++ix){
						float dx2=dx*dx,dy2=dy*dy;
						float d2=dx2+dy2;
						if(d2<r2){
							ReviewFld(j);
							flds[j+cmp]+=(1.0f-d2/r2)*amp;
						}
						j+=x_step;
						dx+=1.0f;
					}
					dy+=1.0f;
					i+=y_step;
				}
			}
			
			public void U_(/*ref Ip ip*/){
				float dt=ip.dtf;
				switch(pc.cs.tag){
				case PC.CS.T.W:
				case PC.CS.T.D:
					dt*=0.1f;
					break;
				}
				foreach(var p in active_pjs){
					pjs[p].rbws.rb.p+=pjs[p].rbws.rb.v*dt;
				}
				var node = active_pjs.First;  
				while (node != null) {
					var next = node.Next;     
					if (pjs[node.Value].t1 <ip.t) {
						unused_pjs.AddFirst(node.Value);
						active_pjs.Remove(node);
					}else{
						AddRadialScalar(pjs[node.Value].rbws.rb.p.X,
						               pjs[node.Value].rbws.rb.p.Y,
						               pjs[node.Value].r,
						               pjs[node.Value].dmg*5,
						               (int)F.DMG );
					}
					node = next; 
				} 
				foreach(var p in active_npcs){
					Vector2 pos=npcs[p].rbws.rb.p;
					float s=npcs[p].fv.b;
					float ss=s*0.1f;
					AddRadialForce (pos.X,pos.Y,s,-20*ss*ss*ss);
				}
				switch(pc.ps.tag){
				case PC.PS.T.WPost:
					AddRadialForce (pc.rbws.rb.p.X,pc.rbws.rb.p.Y,48,-800);
					AddRadialScalar (pc.rbws.rb.p.X,pc.rbws.rb.p.Y,48,0.5f,(int)F.DRAG);
					AddRadialScalar (pc.rbws.rb.p.X,pc.rbws.rb.p.Y,100,50,(int)F.DMG);
					break;
				case PC.PS.T.WPre:
					AddRadialForce (pc.rbws.rb.p.X,pc.rbws.rb.p.Y,18,-500);
					break;
				case PC.PS.T.N:
					AddRadialForce (pc.rbws.rb.p.X,pc.rbws.rb.p.Y,6,-500);
					break;
				}
				Vector2 boxMin=new Vector2(-250,-120);
				Vector2 boxMax=new Vector2(250,120);
				foreach(var p in active_npcs){
					Vector2 pos=npcs[p].rbws.rb.p;
					Sampler smp;
					GetSampler (pos.X,pos.Y,out smp);
					var push_f=new Vector2(Sample (ref smp,(int)F.FORCE_X),Sample (ref smp,(int)F.FORCE_Y));
					var drag_a=npcs[p].rbws.rb.v*(Sample (ref smp,(int)F.DRAG)+0.1f);
					npcs[p].rbws.rb.v-=30*(push_f*dt/npcs[p].fv.b+drag_a)*dt;
					npcs[p].rbws.rb.p+=10*npcs[p].rbws.rb.v*dt;
					npcs[p].rbws.rb.p=npcs[p].rbws.rb.p.Clamp(boxMin,boxMax);
					Vector2 dir = (pc.rbws.rb.p-npcs[p].rbws.rb.p);
					Vector2 newR=-dir.Perpendicular();
					if(newR.LengthSquared()>=0.01f){
						//npcs[p].rbws.rb.av+=1.0f*(npcs[p].fv.r)*(0.5f-(float)ip.sr.NextDouble())*dt;
						//npcs[p].rbws.rb.r += 0.50f*npcs[p].rbws.rb.r.Perpendicular()*npcs[p].rbws.rb.av*dt;
						npcs[p].rbws.rb.r-=npcs[p].rbws.rb.r.Perpendicular()*2.0f*newR.Angle(npcs[p].rbws.rb.r)*dt;
						npcs[p].rbws.rb.r = npcs[p].rbws.rb.r.Normalize();
						npcs[p].rbws.rb.v+= 1.0f*npcs[p].fv.a*npcs[p].rbws.rb.r.Perpendicular()*dt;
					}
					
					npcs[p].d.hp-=Sample (ref smp,(int)F.DMG)*dt;
					if((npcs[p].rbws.rb.p-pc.rbws.rb.p).LengthSquared()<=npcs[p].fv.b*npcs[p].fv.b*0.2f+25){
						pc.d.hp-=20*dt;
					}
				}
				
				node = active_npcs.First;  
				while (node != null) {
					var next = node.Next;     
					if (npcs[node.Value].d.hp <=0) {
						unused_npcs.AddFirst(node.Value);
						active_npcs.Remove(node);
					}
					node = next; 
				} 
				switch(pc.ps.tag){
				case PC.PS.T.N:
					break;
				case PC.PS.T.C:
					break;
				case PC.PS.T.WPre:
					if(ip.t-pc.ps._wpre.t_start>=300){
						Vector2 d=pc.ps._wpre.target-pc.rbws.rb.p;
						pc.rbws.rb.p=pc.ps._wpre.target;
						pc.aim+=d;
						pc.ps.tag=PC.PS.T.WPost;
						pc.ps._wpost.t_start=ip.t;
					}
					break;
				case PC.PS.T.WPost:
					if(ip.t-pc.ps._wpost.t_start>=200){
						pc.ps.tag=PC.PS.T.N;
						pc.ps._n.atks.atking=false;
						pc.ps._n.atks.lat=0;
					}
					break;
				}
				switch(pc.cs.tag){
				case PC.CS.T.N:
					if(PC.PS.T.N==pc.ps.tag){
						if(ip.td.Count>0){
							Vector2 p=vc.cam.GetTouchPos()-pc.rbws.rb.p;
							if(p.LengthSquared()>=25.0f){
								pc.aim = pc.rbws.rb.p-p;
								pc.rbws.rb.r = p.Normalize().Perpendicular();
							}
						}
						//buttons here
						if((((int)ip.gpd.ButtonsDown)&((int)GamePadButtons.R))!=0){
							pc.cs.tag = PC.CS.T.W;
						}/*else if((((int)ip.gpd.ButtonsDown)&((int)GamePadButtons.L))!=0){
							pc.cs.tag = PC.CS.T.D;
						}*/else{
							int k=-1;
							if((((int)ip.gpd.Buttons)&
						          (((int)GamePadButtons.Left)|((int)GamePadButtons.Circle))
								)!=0){
								k=0;
							}else if((((int)ip.gpd.Buttons)&
							          (((int)GamePadButtons.Right)|((int)GamePadButtons.Square))
								)!=0){
								k=1;
							}else if((((int)ip.gpd.Buttons)&
							          (((int)GamePadButtons.Down)|((int)GamePadButtons.Cross))
								)!=0){
								k=2;
							}else if((((int)ip.gpd.Buttons)&
							          (((int)GamePadButtons.Up)|((int)GamePadButtons.Triangle))
								)!=0){
								k=3;
							}
							if(k<0) break;
							int i=unused_pjs.First.Value;
							
							pjs[i].lb=0;
							pjs[i].rbws.rb.p=pc.rbws.rb.p+pc.rbws.rb.r.Perpendicular()*6;
							pjs[i].rbws.rb.r=pc.rbws.rb.r;
							pjs[i].rbws.rb.av=0;
							pjs[i].rbws.rb.v=pc.rbws.rb.r.Perpendicular();
							int cd = 100;
							pjs[i].t0=ip.t;
							pjs[i].t1=ip.t;
							pjs[i].rbws.s=new RBWS.S();
							switch(k){
							case 0:
								cd = 50;
								pjs[i].rbws.rb.v=pjs[i].rbws.rb.v*100+pc.rbws.rb.r*30.0f*(0.5f-(float)ip.sr.NextDouble());
								pjs[i].dmg=25;
								pjs[i].r=3;
								pjs[i].sz=new Vector2(4,4);
								pjs[i].i=0;
								pjs[i].t1+=1000;
								break;
							case 1:
								cd = 600;
								pjs[i].rbws.rb.v=pjs[i].rbws.rb.v*200;
								pjs[i].dmg=100;
								pjs[i].r=10;
								pjs[i].sz=new Vector2(12,12);
								pjs[i].i=1;
								pjs[i].t1+=500;
								break;
							case 2:
								cd = 300;
								pjs[i].rbws.rb.p+=pjs[i].rbws.rb.v*1;
								pjs[i].rbws.rb.v=pjs[i].rbws.rb.v*500;
								pjs[i].dmg=400;
								pjs[i].r=4;
								pjs[i].sz=new Vector2(4,20);
								pjs[i].i=3;
								pjs[i].t1+=200;
								break;
							case 3:
								cd = 1100;
								pjs[i].rbws.rb.v=pjs[i].rbws.rb.v*50;
								pjs[i].dmg=75;
								pjs[i].r=8;
								pjs[i].sz=new Vector2(8,8);
								pjs[i].i=2;
								pjs[i].t1+=2000;
								break;
							}
							if(ip.t-pc.ps._n.atks.lat>=cd && unused_pjs.Count>0){
								pc.ps._n.atks.lat=ip.t;
								unused_pjs.RemoveFirst();
								active_pjs.AddFirst(i);
							}
						}
					}
					break;
				case PC.CS.T.D:
					if(ip.td.Count>0){
						//画符
					}
					break;
				case PC.CS.T.W:
					if(ip.td.Count>0){
						Vector2 p=vc.cam.GetTouchPos();
						p.X=System.Math.Min (System.Math.Max (-250,p.X),250);
						p.Y=System.Math.Min (System.Math.Max (-120,p.Y),120);
						pc.ps.tag=PC.PS.T.WPre;
						pc.ps._wpre.t_start=ip.t;
						pc.ps._wpre.target=p;
						pc.cs.tag=PC.CS.T.N;
					}
					break;
				}
			}
			public void R_(){
				//foreach(var p in npcs){
					//p.Value.rbws.rb.position;
				//}
				
				//foreach(var p in pjs){
					//p.Value.rbws.;
				//}
				vc.cam.Center=0.5f*(vc.cam.Center+pc.rbws.rb.p);
				vc.cam.Push ();
				Director.Instance.GL.SetBlendMode(BlendMode.Normal);
				//vc.cam.DebugDraw(0.1f);
				TRS trs;
				var sr=Director.Instance.SpriteRenderer;
				sr.BeginSprites(vc.ti_units,active_npcs.Count+20);
				trs.T=new Vector2(0.0f,0.0f);
				trs.R=new Vector2(1.0f,0.0f);
				trs.S=new Vector2(1.0f,1.0f);
				if(PC.PS.T.WPost==pc.ps.tag||PC.PS.T.WPre==pc.ps.tag){
					trs.T=pc.rbws.rb.p;
					trs.R=new Vector2(1,0);
					int i=0;
					if(PC.PS.T.WPost==pc.ps.tag){
						trs.S.X=trs.S.Y=0.3f*(float)(ip.t-pc.ps._wpost.t_start);
						i=1;
					}else{
						trs.S.X=trs.S.Y=0.1f*(float)(400-(ip.t-pc.ps._wpre.t_start));
					}
					trs.T-=0.5f*trs.S;
					sr.AddSprite(ref trs,new Vector2i(15,i));
				}
				trs.T=pc.rbws.rb.p;
				trs.R=pc.rbws.rb.r;
				trs.S=new Vector2(10.0f);
				trs.T-=0.5f*(trs.R.Perpendicular()*trs.S.Y+trs.R*trs.S.X);
				{
					int i=3-System.Math.Min (3,System.Math.Max(0,(int)(pc.d.hp/pc.d.hpm*3.8)));
					sr.AddSprite(ref trs,new Vector2i(i,0));
				}
				foreach(var p in active_npcs){
					trs.T=npcs[p].rbws.rb.p;
					trs.R=npcs[p].rbws.rb.r;
					trs.S.X=trs.S.Y=npcs[p].fv.b;
					trs.T-=0.5f*(trs.R.Perpendicular()*trs.S.Y+trs.R*trs.S.X);
					//trs.T-=0.5f*trs.S.X;
					int i=3-System.Math.Min (3,System.Math.Max(0,(int)(npcs[p].d.hp/npcs[p].d.hpm*3.8)));
					sr.AddSprite(ref trs,new Vector2i(i,npcs[p].cid));
				}
				if(PC.CS.T.N==pc.cs.tag&&PC.PS.T.N==pc.ps.tag){
					trs.T=pc.aim-new Vector2(8,8);
					trs.S.X=trs.S.Y=16;
					trs.R.X=1;
					trs.R.Y=0;
					sr.AddSprite(ref trs,new Vector2i(15,15));
				}
				sr.EndSprites();
				
				//Director.Instance.GL.SetBlendMode(BlendMode.Additive);
				sr.BeginSprites (vc.ti_prjs,active_pjs.Count);
				foreach(var p in active_pjs){
					trs.T=pjs[p].rbws.rb.p;
					trs.R=pjs[p].rbws.rb.r;
					trs.S=pjs[p].sz;
					trs.T-=0.5f*(trs.R.Perpendicular()*trs.S.Y+trs.R*trs.S.X);
					sr.AddSprite(ref trs,new Vector2i(0,pjs[p].i));
				}
				sr.EndSprites();
				Director.Instance.GL.SetBlendMode(BlendMode.Normal);
				//sr.DrawTextDebug(""+npcs.Count,new Vector2(0,0),8);
				vc.cam.Pop ();
			}
		};
		public struct App{
			public enum T{ MM,G };
			public T tag;
			public struct _G{
				public W w;
				public struct CS{
					public enum T{ Pr,L,Pa,F };
					public T tag;
				};
				public CS cs;
				public void U_(){
					switch(cs.tag){
					case CS.T.Pr:
						break;
					case CS.T.L:
						w.U_ ();
						break;
					case CS.T.Pa:
						break;
					case CS.T.F:
						break;
					}
				}
				public void R_(){
					a._g.w.R_();
					switch(cs.tag){
					case CS.T.Pr:
						break;
					case CS.T.Pa:
						break;
					case CS.T.F:
						break;
					}
				}
			};
			public _G _g;
		};
		static App a;
		static Ip ip;
		static Timer tmr;
		static TextureInfo ti_banner;
		public static void Initialize ()
		{
			//List<int>
			// Set up the graphics system
			//Log.SetToConsole();
			vc = new VC();
			ip = new Ip();
			tmr = new Timer();
			ip.sr = new System.Random();

			graphics = new GraphicsContext ();
			Director.Initialize(1024,32,graphics);
			
			a._g.w.shs=new Dm.Sh[max_Shs];
			a._g.w.npcs=new NPC[max_NPCs];
			a._g.w.pjs=new Pj[max_Pjs];
			a._g.w.flds=new float[W.flds_length];
			a._g.w.active_npcs=new LinkedList<int>();
			a._g.w.active_pjs=new LinkedList<int>();
			a._g.w.active_shs=new LinkedList<int>();
			a._g.w.unused_npcs=new LinkedList<int>();
			a._g.w.unused_pjs=new LinkedList<int>();
			a._g.w.unused_shs=new LinkedList<int>();
			
//			var t = new Texture2D("/Application/king_water_drop.png",false);
			//var texture_info = new TextureInfo( new Texture2D("/Application/assets/crystals.png", false ) );	
			vc.ti_units=new TextureInfo(new Texture2D("/Application/units.png",true),new Vector2i(16,16));
			vc.ti_prjs=new TextureInfo(new Texture2D("/Application/prjs.png",true),new Vector2i(16,16));
			ti_banner=new TextureInfo(new Texture2D("/Application/banner.png",true),new Vector2i(1,1));
			vc.cam=new Camera2D(Director.Instance.GL,Director.Instance.DrawHelpers);
			//vc.cam.SetViewFromViewport();
			
			
			//on_start
			a.tag=App.T.G;
			a._g.cs.tag=App._G.CS.T.L;
			System.Array.Clear(a._g.w.flds,0,W.flds_length);
			a._g.w.active_npcs.Clear();
			a._g.w.active_pjs.Clear();
			a._g.w.active_shs.Clear();
			for(int i=0;i<max_NPCs;++i) a._g.w.unused_npcs.AddLast(i);
			for(int i=0;i<max_Pjs;++i) a._g.w.unused_pjs.AddLast(i);
			for(int i=0;i<max_Shs;++i) a._g.w.unused_shs.AddLast(i);
			a._g.w.pc.aim=new Vector2(0,20);
			a._g.w.pc.rbws.rb.p=new Vector2(0,0);
			a._g.w.pc.rbws.rb.r=new Vector2(1,0);
			a._g.w.pc.ps.tag=PC.PS.T.N;
			a._g.w.pc.ps._n.atks.atking=false;
			a._g.w.pc.ps._n.atks.lat=0;
			a._g.w.pc.d.hp=1000;
			a._g.w.pc.d.hpm=1000;
			a._g.w.pc.d.sh_count=0;
			a._g.w.pc.cs.tag=PC.CS.T.N;
		}
	
		public static void Update ()
		{
			// Query gamepad for current state
			ip.gpd = GamePad.GetData (0);
			ip.td = Touch.GetData(0);
			ip.vp2w = new Vector2(0,0);
			ip.vp2w_s = 0;
			{
				double t=tmr.Milliseconds();
				TM ti=(TM)t;
				ip.dtf=(ti-ip.t)*0.001f;
				ip.t=ti;
			}
			++ip.cur_frame;
			ip.cur_frame_f=(float)ip.cur_frame;
			
			switch(a.tag){
			case App.T.MM:
				break;
			case App.T.G:
				a._g.U_();
				break;
			}
			
			if(ip.t>=a._g.w.lastSpawn+300){
				a._g.w.lastSpawn+=300;
				NPC npc=new NPC();
				npc.fv.b=6+12*(float)System.Math.Pow (ip.sr.NextDouble(),4);
				npc.fv.r=4+24*(float)System.Math.Pow (ip.sr.NextDouble(),1);
				npc.fv.a=5+18*(float)System.Math.Pow (ip.sr.NextDouble(),3);
				npc.cid=6;
				if(npc.fv.b>16){
					npc.cid=5;
				}else if(npc.fv.r>20){
					npc.cid=4;
				}else if(npc.fv.a>20){
					npc.cid=1;
				}else if(npc.fv.a>16){
					npc.cid=3;
				}else if(npc.fv.r>15){
					npc.cid=2;
				}
				npc.d.hp=npc.d.hpm=5*npc.fv.b;
				npc.d.sh_count=0;
				npc.fac=1;
				npc.ps.atks.atking=false;
				npc.ps.atks.lat=0;
				npc.rbws.rb.p=new Vector2((0.5f-(float)ip.sr.NextDouble())*480,(0.5f-(float)ip.sr.NextDouble())*240);
				do{
					npc.rbws.rb.r=new Vector2((float)ip.sr.NextDouble()-0.5f,(float)ip.sr.NextDouble()-0.5f);
				}while(npc.rbws.rb.r.LengthSquared()<0.01f);
				npc.rbws.rb.r=npc.rbws.rb.r.Normalize();
				npc.rbws.s=new RBWS.S();
				int i=a._g.w.unused_npcs.First.Value;
				if(a._g.w.unused_npcs.Count>0){
					a._g.w.unused_npcs.RemoveFirst();
					a._g.w.npcs[i]=npc;
					a._g.w.active_npcs.AddLast(i);
				}
			}
			//System.Array.Clear(a._g.w.flds,0,W.flds_length);
		}

		public static void Render ()
		{
			// Clear the screen
			graphics.SetClearColor (0.77f, 0.75f, 0.7f, 0.7f);
			graphics.Clear ();
			vc.cam.SetAspectFromViewport();
			vc.cam.SetViewFromHeightAndCenter(100,a._g.w.pc.rbws.rb.p);
			switch(a.tag){
			case App.T.MM:
				break;
			case App.T.G:
				a._g.R_();
				break;
			}
		
			//vSetViewFromHeightAndCenterViewport();
			//vc.cam.DebugDraw(2.0f);
			/*
			vc.cam.Push ();
			Director.Instance.GL.SetBlendMode(BlendMode.Normal);
			//vc.cam.DebugDraw(0.1f);
			var sr=Director.Instance.SpriteRenderer;
			//Vector4 col = Math.SetAlpha( Colors.White, 0.5f );
			//sr.DefaultShader.SetColor( ref col );
			//sr.DefaultShader.SetUVTransform( ref Math.UV_TransformFlipV );
			sr.DrawTextDebug("1245",new Vector2(0,0),8);
			//Vector4 col = Math.SetAlpha( Colors.White, 0.5f );
			//Director.Instance.SpriteRenderer.DefaultShader.SetColor( ref col );
			sr.BeginSprites(vc.ti_units,2);
			//sr.FlipU = false;
			//sr.FlipV = false;
			TRS trs;
			trs.T=new Vector2(-50,0);
			trs.R=new Vector2(1,0);
			trs.S=new Vector2(10,10);
			sr.AddSprite(ref trs,new Vector2i(4,6));
			trs.T.X=50;
			sr.AddSprite(ref trs,new Vector2i(0,6));
			sr.EndSprites();
			vc.cam.Pop ();*/
			//Director.Instance.Update();
			//Director.Instance.Render();
			vc.cam.SetViewFromViewport();
			vc.cam.Push();
			Director.Instance.SpriteRenderer.BeginSprites(ti_banner,1);
			TRS trs;
			trs.T.X=trs.T.Y=0;
			trs.R.X=1;
			trs.R.Y=0;
			trs.S.X=1;//250;
			trs.S.Y=1;//50;
			Director.Instance.SpriteRenderer.AddSprite(ref trs,new Vector2i(0,0));
			Director.Instance.SpriteRenderer.EndSprites();

			vc.cam.Pop ();
			vc.cam.SetAspectFromViewport();
			vc.cam.SetViewFromHeightAndCenter(100,a._g.w.pc.rbws.rb.p);
			Director.Instance.GL.Context.SwapBuffers();
			Director.Instance.PostSwap();
			// Present the screen
			//graphics.SwapBuffers ();
		}
	}
}
