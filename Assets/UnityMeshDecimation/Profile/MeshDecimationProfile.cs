using UnityEngine;
using UnityMeshDecimation.Internal;
using UnityMesh = UnityEngine.Mesh;

namespace UnityMeshDecimation {
    public abstract class MeshDecimationProfile : ScriptableObject {

        [SerializeField]
        public EdgeCollapseParameter parameter;

        private UnityMeshDecimation meshDecimation;

        public MeshDecimationProfile() {
            this.parameter = new EdgeCollapseParameter();
        }

        public UnityMesh Optimize(UnityMesh inputMesh, TargetConditions targetCoditions, UnityMesh outputMesh = null) {
            if(!this.BeforeOptimize()) {
                return null;
            }

            if(this.meshDecimation == null) {
                this.meshDecimation = new UnityMeshDecimation();
            }
            this.meshDecimation.Execute(inputMesh, this.parameter, targetCoditions, true);

            this.AfterOptimize();

            if(outputMesh != null) {
                this.meshDecimation.ToMesh(outputMesh);
                return outputMesh;
            }
            else {
                return this.meshDecimation.ToMesh();
            }
        }

        public virtual bool BeforeOptimize() {
            return true;
        }

        public virtual void AfterOptimize() {

        }

    }
}
