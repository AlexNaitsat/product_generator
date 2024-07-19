#!/bin/bash

# Function to display usage
usage() {
    echo "Usage: $0 <source> <target> <material_list> <cameras_list>"
    echo "  <source>        : Source dataset root directory"
    echo "  <target>        : Target dataset root directory"
    echo "  <material_list> : Comma-separated list of materials (e.g., 'Original,NOC,SegmentationMask')"
    echo "  <cameras_list>  : Comma-separated list of cameras (e.g., 'y00,y01')"
    exit 1
}

# Check if the number of arguments is correct
if [ "$#" -ne 4 ]; then
    usage
fi

SOURCE=$1
TARGET=$2
MATERIAL_LIST=$3
CAMERAS_LIST=$4

IFS=',' read -ra MATERIALS <<< "$MATERIAL_LIST"
IFS=',' read -ra CAMERAS <<< "$CAMERAS_LIST"

# Function to generate copy commands
generate_copy_commands() {
    local dataset_type=$1
    local source=$2
    local target=$3

    for prefab in "$source/$dataset_type"/*; do
        if [ -d "$prefab" ]; then
            prefab_name=$(basename "$prefab")
            target_dir="$target/$dataset_type/$prefab_name"
            if [ ! -d "$target_dir" ]; then
                echo "mkdir -p \"$target_dir\""
            fi
            for material in "${MATERIALS[@]}"; do
                for camera in "${CAMERAS[@]}"; do
                    find "$prefab" -name "frame_*_${material}_${camera}.png" | while read -r file; do
                        target_file="$target_dir/$(basename "$file")"
                        echo "cp \"$file\" \"$target_file\""
                    done
                done
            done
        fi
    done
}


# Generate copy commands for train and test datasets
generate_copy_commands "train" "$SOURCE" "$TARGET"
generate_copy_commands "test" "$SOURCE" "$TARGET"
