$(window).on('load', async function () {
    class JointJsController {
        graph = undefined;
        paper = undefined;
        namespaces = undefined;
        objects = undefined;
        links = undefined;

        makeNamespace({namespace}) {
            return new joint.shapes.standard.HeaderedRectangle({
                position: {x: 50, y: 50},
                size: {width: 300, height: 300},
                attrs: {
                    headerText: {
                        text: namespace
                    },
                    body: {
                        fill: 'lightblue',
                        stroke: 'black',
                        strokeWidth: 1
                    },
                },
                fitEmbeds: true
            });
        }

        makeClass({namespace, name, width, height, type}) {
            let fill = 'lightgreen';
            
            if(type === 0){
                fill = 'lightyellow';
            }
            
            const classObj = new joint.shapes.uml.Class({
                position: {x: 70, y: 80},
                size: {width: width, height: height},
                name: name,
                attrs: {
                    label: {text: name, fill: 'black', fontSize: 15},
                    body: {fill: 'lightgreen'}
                }
            });
            const namespaceObj = this.namespaces[namespace];
            namespaceObj.embed(classObj);
            return classObj;
        }

        makeAssociation({dependant, dependsOn}) {
            return new joint.shapes.standard.Link({
                source: dependant,
                target: dependsOn,
            });
        }

        makeGeneralization({child, base}) {
            return new joint.shapes.standard.Link({
                source: child,
                target: base,
            });
        }

        constructCells({treeData}) {
            this.namespaces = {};
            this.objects = {};
            this.links = [];

            for (const obj of treeData) {
                const rootNode = obj.rootNode;
                if (!(rootNode.namespace in this.namespaces)) {
                    const namespace = this.makeNamespace({namespace: rootNode.namespace});
                    this.namespaces[rootNode.namespace] = namespace;
                    namespace.addTo(this.graph);
                }
            }

            for (const obj of treeData) {
                const rootNode = obj.rootNode;
                const relations = obj.related;
                let classObj = this.makeClass({
                    namespace: rootNode.namespace,
                    name: rootNode.typeName,
                    width: 100,
                    height: 80,
                    type: rootNode.nodeType
                });
                this.objects[rootNode.fullName] = classObj;
                classObj.addTo(this.graph);
                for (const relation of relations) {
                    if (!(relation.fullName in this.objects)) {
                        classObj = this.makeClass({
                            namespace: relation.namespace,
                            name: relation.typeName,
                            width: 100,
                            height: 80,
                            type: relation.nodeType
                        });
                        this.objects[relation.fullName] = classObj;
                        classObj.addTo(this.graph);
                    }

                    if (relation.dependencyType === 1) {
                        const association = this.makeAssociation({
                            dependant: this.objects[rootNode.fullName],
                            dependsOn: this.objects[relation.fullName]
                        });
                        this.links.push(association);
                        association.addTo(this.graph);
                    }

                    if (relation.dependencyType === 2) {
                        const generalization = this.makeGeneralization({
                            child: this.objects[rootNode.fullName],
                            base: this.objects[relation.fullName]
                        });
                        this.links.push(generalization);
                        generalization.addTo(this.graph);
                    }
                }
            }
        }

        bindPaperZoom() {
            const offsetToLocalPoint = (x, y) => {
                var svgPoint = this.paper.svg.createSVGPoint();
                svgPoint.x = x;
                svgPoint.y = y;

                var pointTransformed = svgPoint.matrixTransform(this.paper.viewport.getCTM().inverse());
                return pointTransformed;
            }
            
            let scaling = 1;
            const maxScale = 5;
            const minScale = 0.2;
            this.paper.$el.on('wheel', (event) => {
                 // scale with the origin of the transformation at point `x=100` and `y=100`

                event.preventDefault(); // Prevent default scrolling behavior

                const deltaY = event.originalEvent.deltaY; // Scroll delta on Y-axis

                if (deltaY < 0) {
                    scaling += 0.1;
                    this.paper.scale(scaling, scaling);
                } else if (deltaY > 0) {
                    scaling -= 0.1;
                    this.paper.scale(scaling, scaling);
                }
                
                if(scaling > maxScale || scaling < minScale){
                    scaling = 1;
                    this.paper.scale(1);
                    this.paper.setOrigin(0,0);
                }
            });
        }

        bindPaperMove() {
            // Track mouse movement and move paper while holding left mouse button
            let isDragging = false;
            let lastMousePosition = {x: 0, y: 0};

            const diagramRef = $('#diagram');

            diagramRef.on('mousedown', (event) => {
                if (event.which === 2) {
                    isDragging = true;
                    lastMousePosition = {x: event.pageX, y: event.pageY};
                }
            });

            diagramRef.on('mousemove', (event) => {
                if (isDragging) {
                    let dx = event.pageX - lastMousePosition.x;
                    let dy = event.pageY - lastMousePosition.y;
                    lastMousePosition = {x: event.pageX, y: event.pageY};

                    let currentTranslate = this.paper.translate();
                    this.paper.translate(currentTranslate.tx + dx, currentTranslate.ty + dy);
                }
            });

            diagramRef.on('mouseup', (event) => {
                if (event.which === 2) {
                    isDragging = false;
                }
            });
        }

        async init() {
            const treeData = await loadTree();

            this.graph = new joint.dia.Graph();
            this.paper = new joint.dia.Paper({
                el: document.getElementById('diagram'),
                model: this.graph,
                width: '100%',
                height: '90vh',
                gridSize: 10,
                drawGrid: true
            });

            this.constructCells({treeData});
            let graph = this.graph;

            joint.layout.DirectedGraph.layout(graph, {setLinkVertices: false});

            this.bindPaperMove();
            this.bindPaperZoom();
        }
    }

    const controller = new JointJsController();
    await controller.init();
});