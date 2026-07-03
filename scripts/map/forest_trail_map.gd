@tool
extends Node2D

const TILE_WIDTH := 76.0

@export var map_half_width := 900.0
@export var map_height := 6500.0
@export var bottom_visual_extension := 1400.0
@export var path_clearance_tiles := 5
@export var viewport_half_width := 960.0
@export var thicket_overflow := 360.0
@export var tree_row_spacing := 42.0
@export var tree_columns_per_side := 8
@export var path_detail_spacing := 95.0

@export_group("Инструменты")
@export var rebuild_map := false:
	set(value):
		rebuild_map = false
		if value:
			remove_meta("map_built")
			call_deferred("_build_map")

@onready var _floor: Polygon2D = $Ground/ForestFloor
@onready var _trees: Node2D = $Decorations/Trees
@onready var _bushes: Node2D = $Decorations/Bushes
@onready var _path_details: Node2D = $Decorations/PathDetails
@onready var _collisions: StaticBody2D = $Collisions
@onready var _left_wall: CollisionShape2D = $Collisions/LeftForestWall
@onready var _right_wall: CollisionShape2D = $Collisions/RightForestWall
@onready var _bottom_wall: CollisionShape2D = $Collisions/BottomBarrier

var _tree_textures: Array[Texture2D] = [
	preload("res://art/tree/tr1.png"),
	preload("res://art/tree/tr2.png"),
	preload("res://art/tree/tr3.png"),
	preload("res://art/tree/tr4.png"),
	preload("res://art/tree/tr5.png"),
	preload("res://art/tree/tr6.png"),
	preload("res://art/tree/tr7.png"),
	preload("res://art/tree/tr8.png"),
]

var _bush_textures: Array[Texture2D] = [
	preload("res://art/Bush/ks1.png"),
	preload("res://art/Bush/ks2.png"),
	preload("res://art/Bush/ks33.png"),
	preload("res://art/Bush/ks4.png"),
]

var _grass_textures: Array[Texture2D] = [
	preload("res://art/Grass/gr1.png"),
	preload("res://art/Grass/gr2.png"),
	preload("res://art/Grass/gr3.png"),
	preload("res://art/Grass/gr4.png"),
]

var _stone_textures: Array[Texture2D] = [
	preload("res://art/stones/st1.png"),
	preload("res://art/stones/st2.png"),
	preload("res://art/stones/st3.png"),
]

var _branch_textures: Array[Texture2D] = [
	preload("res://art/other/ot8.png"),
	preload("res://art/other/ot12.png"),
	preload("res://art/other/ot13.png"),
	preload("res://art/other/ot14.png"),
]

var _stump_textures: Array[Texture2D] = [
	preload("res://art/stump/pn1.png"),
	preload("res://art/stump/pn2.png"),
]


func _ready() -> void:
	if not _should_auto_build():
		return
	call_deferred("_build_map")


func _should_auto_build() -> bool:
	if not Engine.is_editor_hint():
		return true
	return get_tree().edited_scene_root == self


func _build_map() -> void:
	if not is_inside_tree():
		return
	if not _ensure_map_nodes():
		return

	_build_floor()
	_build_thicket()
	_build_path_details()
	_build_collisions()
	_position_landmarks()
	set_meta("map_built", true)


func _ensure_map_nodes() -> bool:
	if _floor == null:
		_floor = get_node_or_null("Ground/ForestFloor") as Polygon2D
		_trees = get_node_or_null("Decorations/Trees") as Node2D
		_bushes = get_node_or_null("Decorations/Bushes") as Node2D
		_path_details = get_node_or_null("Decorations/PathDetails") as Node2D
		_left_wall = get_node_or_null("Collisions/LeftForestWall") as CollisionShape2D
		_right_wall = get_node_or_null("Collisions/RightForestWall") as CollisionShape2D
		_bottom_wall = get_node_or_null("Collisions/BottomBarrier") as CollisionShape2D

	return (
		_floor != null
		and _trees != null
		and _path_details != null
		and _bottom_wall != null
	)


func _path_half_width() -> float:
	return path_clearance_tiles * TILE_WIDTH


func _visual_half_width() -> float:
	return maxf(map_half_width, viewport_half_width + thicket_overflow)


func _visual_bottom() -> float:
	return map_height + bottom_visual_extension


func _build_floor() -> void:
	var w := _visual_half_width()
	var bottom := _visual_bottom()
	_floor.polygon = PackedVector2Array([
		Vector2(-w, -120),
		Vector2(w, -120),
		Vector2(w, bottom + 120),
		Vector2(-w, bottom + 120),
	])
	_floor.color = Color.BLACK


func _build_thicket() -> void:
	_clear_children(_trees)
	_clear_children(_bushes)

	var rng := RandomNumberGenerator.new()
	rng.seed = 67

	var path_half := _path_half_width()
	var visual_half := _visual_half_width()
	var available := visual_half - path_half
	var columns := maxi(
		tree_columns_per_side,
		int(ceil(available / 64.0)) + 2
	)

	var y: float = -180.0
	while y < _visual_bottom() + 180.0:
		for side: int in [-1, 1]:
			for col in columns:
				var depth: float = path_half + 20.0 + float(col) * 64.0
				var x: float = float(side) * (depth + rng.randf_range(-16.0, 16.0))
				if absf(x) > visual_half - 16.0:
					continue

				var tree := Sprite2D.new()
				tree.texture = _tree_textures[rng.randi() % _tree_textures.size()]
				tree.position = Vector2(
					x + rng.randf_range(-14.0, 14.0),
					y + rng.randf_range(-16.0, 16.0)
				)
				tree.z_index = 1 + col
				tree.flip_h = side < 0 and rng.randf() > 0.45
				_trees.add_child(tree)
				_set_owner_if_edited(tree)

				if col < 3 and rng.randf() > 0.25:
					var bush := Sprite2D.new()
					bush.texture = _bush_textures[rng.randi() % _bush_textures.size()]
					bush.position = Vector2(
						x + side * rng.randf_range(14.0, 38.0),
						y + rng.randf_range(6.0, 22.0)
					)
					bush.z_index = 2
					_bushes.add_child(bush)
					_set_owner_if_edited(bush)

		y += tree_row_spacing + rng.randf_range(-10.0, 10.0)


const MIN_PROP_DISTANCE := 62.0
const GRASS_PER_SEGMENT := 6
const GRASS_Y_SPREAD := 44.0
const GRASS_Y_JITTER := 22.0

func _build_path_details() -> void:
	_clear_children(_path_details)

	var rng := RandomNumberGenerator.new()
	rng.seed = 131

	var path_half := _path_half_width()
	var inner_margin := 32.0
	var placed: Array[Vector2] = []
	var grass_bag: Array[int] = []
	var y: float = 80.0
	var visual_bottom := _visual_bottom()

	while y < visual_bottom - 60.0:
		for _grass_i in GRASS_PER_SEGMENT:
			var grass_y: float = y + rng.randf_range(-GRASS_Y_SPREAD, GRASS_Y_SPREAD)
			var grass_pos := _find_free_position(
				rng, placed, path_half, inner_margin, grass_y, GRASS_Y_JITTER
			)
			if grass_pos == Vector2.INF:
				continue
			var grass := Sprite2D.new()
			grass.texture = _pick_grass_texture(rng, grass_bag)
			grass.flip_h = rng.randf() > 0.5
			grass.position = grass_pos
			grass.z_index = -2
			_path_details.add_child(grass)
			_set_owner_if_edited(grass)
			placed.append(grass_pos)

		if rng.randf() < 0.38:
			var extra := _create_path_prop(rng)
			var extra_pos := _find_free_position(
				rng, placed, path_half, inner_margin, y, 10.0
			)
			if extra_pos != Vector2.INF:
				extra.position = extra_pos
				extra.z_index = -2
				_path_details.add_child(extra)
				_set_owner_if_edited(extra)
				placed.append(extra_pos)

		y += path_detail_spacing + rng.randf_range(-20.0, 20.0)


func _pick_grass_texture(
	rng: RandomNumberGenerator,
	grass_bag: Array[int]
) -> Texture2D:
	if grass_bag.is_empty():
		for i in range(_grass_textures.size()):
			grass_bag.append(i)
		_shuffle_array(rng, grass_bag)
	return _grass_textures[grass_bag.pop_back()]


func _shuffle_array(rng: RandomNumberGenerator, values: Array[int]) -> void:
	for i in range(values.size() - 1, 0, -1):
		var j := rng.randi_range(0, i)
		var tmp: int = values[i]
		values[i] = values[j]
		values[j] = tmp


func _find_free_position(
	rng: RandomNumberGenerator,
	placed: Array[Vector2],
	path_half: float,
	inner_margin: float,
	center_y: float,
	max_y_jitter: float,
	max_attempts: int = 18
) -> Vector2:
	for _attempt in max_attempts:
		var pos := Vector2(
			rng.randf_range(-path_half + inner_margin, path_half - inner_margin),
			center_y + rng.randf_range(-max_y_jitter, max_y_jitter)
		)
		if _is_position_free(pos, placed):
			return pos
	return Vector2.INF


func _is_position_free(pos: Vector2, placed: Array[Vector2]) -> bool:
	for existing in placed:
		if existing.distance_to(pos) < MIN_PROP_DISTANCE:
			return false
	return true


func _create_big_bush(rng: RandomNumberGenerator) -> Node2D:
	var texture: Texture2D = _bush_textures[rng.randi() % _bush_textures.size()]
	var bush_root := Node2D.new()

	var sprite := Sprite2D.new()
	sprite.texture = texture
	bush_root.add_child(sprite)

	var big_bush := Area2D.new()
	big_bush.name = &"BigBush"
	big_bush.collision_layer = 2
	big_bush.collision_mask = 0
	big_bush.monitorable = true
	big_bush.add_to_group("big_bush")
	bush_root.add_child(big_bush)

	var collision := CollisionShape2D.new()
	collision.name = &"BigBush"
	var rect := RectangleShape2D.new()
	var bush_size := texture.get_size()
	rect.size = bush_size * 1.35
	collision.shape = rect
	big_bush.add_child(collision)

	return bush_root


func _create_path_prop(rng: RandomNumberGenerator) -> Node2D:
	var roll := rng.randf()
	var sprite := Sprite2D.new()

	# Трава размещается отдельным циклом; здесь только остальные объекты.
	# Камни: 9% (вдвое меньше прежних 18%).
	if roll < 0.47:
		return _create_big_bush(rng)
	elif roll < 0.56:
		sprite.texture = _stone_textures[rng.randi() % _stone_textures.size()]
	elif roll < 0.81:
		sprite.texture = _branch_textures[rng.randi() % _branch_textures.size()]
	else:
		sprite.texture = _stump_textures[rng.randi() % _stump_textures.size()]

	return sprite


func _clear_children(node: Node2D) -> void:
	for child in node.get_children():
		node.remove_child(child)
		child.free()


func _set_owner_if_edited(node: Node) -> void:
	if Engine.is_editor_hint() and is_inside_tree():
		var root := get_tree().edited_scene_root
		if root:
			node.owner = root


func _build_collisions() -> void:
	var path_half := _path_half_width()
	var wall_width := map_half_width - path_half
	var left_shape := _left_wall.shape as RectangleShape2D
	var right_shape := _right_wall.shape as RectangleShape2D
	left_shape.size = Vector2(wall_width, map_height)
	right_shape.size = Vector2(wall_width, map_height)
	_left_wall.position = Vector2(-(path_half + map_half_width) * 0.5, map_height * 0.5)
	_right_wall.position = Vector2((path_half + map_half_width) * 0.5, map_height * 0.5)

	var bottom_shape := _bottom_wall.shape as RectangleShape2D
	var path_width := path_half * 2.0
	bottom_shape.size = Vector2(path_width + 48.0, 56.0)
	_bottom_wall.position = Vector2(0.0, map_height - 28.0)


func _position_landmarks() -> void:
	var house_y := 150.0
	var start_y := map_height - 280.0
	var campfire_count := 6
	var campfire_bottom := start_y - 120.0
	var campfire_top := house_y + 250.0
	var campfire_spacing := (campfire_bottom - campfire_top) / float(campfire_count - 1)

	$Landmarks/House.position.y = house_y
	if $Landmarks.has_node("HouseGoal"):
		$Landmarks/HouseGoal.position.y = house_y + 130.0
	var campfires := $Landmarks/Campfires
	for i in campfire_count:
		campfires.get_child(i).position = Vector2(0.0, campfire_bottom - i * campfire_spacing)
	$Landmarks/Start.position.y = start_y
	$Landmarks/StartArea.position.y = start_y
	$Collisions/HouseBlock.position.y = house_y + 10.0

	var clearing_offsets := [
		Vector2(-map_half_width * 0.42, map_height * 0.14),
		Vector2(map_half_width * 0.44, map_height * 0.28),
		Vector2(-map_half_width * 0.4, map_height * 0.46),
		Vector2(map_half_width * 0.43, map_height * 0.62),
	]
	for i in clearing_offsets.size():
		$Decorations/Clearings.get_child(i).position = clearing_offsets[i]

	var path_half := _path_half_width()
	var match_offsets := [
		Vector2(-path_half - 180.0, map_height * 0.08),
		Vector2(path_half + 190.0, map_height * 0.15),
		Vector2(-path_half - 170.0, map_height * 0.24),
		Vector2(path_half + 185.0, map_height * 0.31),
		Vector2(-path_half - 175.0, map_height * 0.4),
		Vector2(path_half + 180.0, map_height * 0.47),
		Vector2(-path_half - 165.0, map_height * 0.56),
		Vector2(path_half + 195.0, map_height * 0.63),
	]
	for i in match_offsets.size():
		$MatchSpawns.get_child(i).position = match_offsets[i]
